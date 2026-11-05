using BankingClient.Api;
using BankingClient.Api.Contracts;
using BankingClient.Authentication;
using BankingClient.Models.Account;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

/// <summary>
/// Handles browser authentication flows while keeping API tokens inside the
/// server-side authentication cookie.
/// </summary>
[Route("account")]
public sealed class AccountController(IBankingApiClient api) : Controller
{
    #region GET Login
    /// <summary>
    /// Login view, providing a form for the user to enter their credentials. This action returns the view with an empty model for the user to fill out, along with any return URL or session expiration information.
    /// </summary>
    /// <param name="returnUrl"></param>
    /// <param name="sessionExpired"></param>
    /// <returns></returns>
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl, bool sessionExpired) => View(new LoginViewModel { ReturnUrl = returnUrl, SessionExpired = sessionExpired });
    #endregion

    #region POST Login
    /// <summary>
    /// Logs the user in by validating their credentials against the banking API, retrieving their profile, and creating a server-side authentication cookie. If the model state is invalid or the API call fails, returns the view with validation errors; otherwise, redirects to the appropriate home page based on the user's role.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var login = await api.LoginAsync(new LoginRequest(model.Email, model.Password), cancellationToken);

        if (login.Value is not { } session)
        {
            ModelState.AddApiProblem(login.Problem!);
            return View(model);
        }

        var profile = await SignInAsync(session, cancellationToken);
        return RedirectToLocal(model.ReturnUrl, profile);
    }
    #endregion

    #region GET Register
    /// <summary>
    /// Returns the registration view, providing a form for the user to create a new account. This action returns the view with an empty model for the user to fill out.
    /// </summary>
    /// <returns></returns>
    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());
    #endregion

    #region POST Register
    /// <summary>
    /// Registers a new user account, validating the input and calling the banking API. If the model state is invalid or the API call fails, returns the view with validation errors; otherwise, signs the user in and redirects to the appropriate home page.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var problem = await api.RegisterAsync(new RegisterRequest(model.Email, model.Password, model.DisplayName), cancellationToken);
        if (problem is not null)
        {
            ModelState.AddApiProblem(problem);
            return View(model);
        }

        // Sign the customer in immediately after registration so a successful account
        // creation does not require a second manual login.
        var login = await api.LoginAsync(new LoginRequest(model.Email, model.Password), cancellationToken);
        if (login.Value is not { } session)
        {
            return RedirectToAction(nameof(Login));
        }

        await SignInAsync(session, cancellationToken);
        return SignedInHome.For(isAdmin: false);
    }
    #endregion

    #region POST Logout
    /// <summary>
    /// Logs the user out by revoking their API token and deleting the server-side authentication cookie. This action ensures that the user's session is terminated both on the client and server sides, preventing any further access to protected resources.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Revoke the API token before deleting the browser session. This ensures that a
        // copied or replayed cookie cannot continue using the API after logout completes.
        await api.LogoutAsync(cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Signs the user in by retrieving their profile from the banking API and creating a server-side authentication cookie. This method handles the API call to get the user's profile, creates the authentication principal and properties, and signs the user in using cookie authentication.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<ProfileResponse> SignInAsync(LoginResponse session, CancellationToken cancellationToken)
    {
        var profile = await api.GetProfileAsync(session.AccessToken, cancellationToken);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClientSession.CreatePrincipal(profile),
            ClientSession.CreateProperties(session));
        return profile;
    }

    /// <summary>
    /// Redirects the user to the appropriate signed-in home page based on their role. If the user is an admin, they are redirected to the admin dashboard; otherwise, they are redirected to the customer dashboard.
    /// </summary>
    /// <param name="returnUrl"></param>
    /// <param name="profile"></param>
    /// <returns></returns>
    private IActionResult RedirectToLocal(string? returnUrl, ProfileResponse profile) => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : SignedInHome.For(profile.Roles.Contains(ClientRoles.Admin));

    /// <summary>
    /// Adds the errors from an API problem result to the model state, allowing them to be displayed in the view. If the problem has no specific field errors, adds a general error message to the model state.
    /// </summary>
    /// <param name="problem"></param>
    private void AddProblemErrors(ApiProblem problem)
    {
        if (problem.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, problem.Title);
            return;
        }

        foreach (var (field, messages) in problem.Errors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(field, message);
            }
        }
    }

    /// <summary>
    /// Redirects the user to the appropriate signed-in home page based on their role. If the user is an admin, they are redirected to the admin dashboard; otherwise, they are redirected to the customer dashboard.
    /// </summary>
    /// <param name="returnUrl"></param>
    /// <returns></returns>
    private IActionResult RedirectToLocal(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToSignedInHome();

    /// <summary>
    /// Redirects the user to the appropriate signed-in home page based on their role. If the user is an admin, they are redirected to the admin dashboard; otherwise, they are redirected to the customer dashboard.
    /// </summary>
    /// <returns></returns>
    private RedirectToActionResult RedirectToSignedInHome() => RedirectToAction(nameof(DashboardController.Index), "Dashboard");
    #endregion
}
