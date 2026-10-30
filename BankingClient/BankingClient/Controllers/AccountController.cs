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
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });
    #endregion

    #region POST Login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        // Validate locally before making the API request so invalid form input is returned
        // immediately with the original model and its validation messages.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var login = await api.LoginAsync(new LoginRequest(model.Email, model.Password), cancellationToken);

        // The API returns expected authentication failures as a problem result. Keep the
        // user on the login page rather than converting those failures into an exception.
        if (login.Value is not { } session)
        {
            ModelState.AddModelError(string.Empty, login.Problem!.Title);
            return View(model);
        }

        await SignInAsync(session, cancellationToken);
        return RedirectToLocal(model.ReturnUrl);
    }
    #endregion

    #region GET Register
    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());
    #endregion

    #region POST Register
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
            AddProblemErrors(problem);
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
        return RedirectToSignedInHome();
    }
    #endregion

    #region POST Logout
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
    // Enrich the cookie with the current profile and token expiration. The cookie is
    // then used by BearerTokenHandler when subsequent requests call the API.
    private async Task SignInAsync(LoginResponse session, CancellationToken cancellationToken)
    {
        var profile = await api.GetProfileAsync(session.AccessToken, cancellationToken);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClientSession.CreatePrincipal(profile),
            ClientSession.CreateProperties(session)
        );
    }

    // API validation keys match the view-model property names, allowing MVC to render
    // each message beside the corresponding input field.
    private void AddProblemErrors(ApiProblem problem)
    {
        if (problem.Errors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, problem.Title);
            return;
        }

        // The API names fields like the view model does (Email, Password), so each
        // error appears beside the field it belongs to.
        foreach (var (field, messages) in problem.Errors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(field, message);
            }
        }
    }

    // Preserve only local return URLs to prevent an external redirect after login.
    private IActionResult RedirectToLocal(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToSignedInHome();

    // Where a user lands after signing in. Phase 4 changes this to the dashboard.
    private RedirectToActionResult RedirectToSignedInHome() => RedirectToAction(nameof(ProfileController.Index), "Profile");
    #endregion
}
