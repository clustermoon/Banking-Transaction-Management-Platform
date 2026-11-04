using BankingAPI.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    CustomerRegistrar customerRegistrar,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AccessTokenIssuer tokenIssuer) : ControllerBase
{
    #region POST REGISTER
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var registration = await customerRegistrar.RegisterAsync(
            request.Email, request.Password, request.DisplayName, cancellationToken);

        if (registration.User is not { } user)
        {
            AddIdentityErrors(registration.Outcome);
            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(user.Id, request.Email, user.DisplayName));
    }
    #endregion

    #region POST LOGIN
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return InvalidCredentials();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Problem(
                title: "Account locked",
                detail: "Too many failed sign-in attempts. Try again in 15 minutes.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        // Issue a token only after the credentials have been fully validated.
        var token = tokenIssuer.Issue(user, await userManager.GetRolesAsync(user));
        return Ok(new LoginResponse(token.Value, token.ExpiresAt));
    }
    #endregion

    #region POST LOGOUT
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var rotated = await userManager.UpdateSecurityStampAsync(user);
        if (!rotated.Succeeded)
        {
            throw new InvalidOperationException("Rotating the security stamp during logout failed.");
        }

        return NoContent();
    }
    #endregion

    #region HELPER METHODS
    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            if (error.Code == nameof(IdentityErrorDescriber.DuplicateUserName))
            {
                continue;
            }

            var field = error.Code.StartsWith("Password", StringComparison.Ordinal) ? nameof(RegisterRequest.Password)
                : error.Code.Contains("Email", StringComparison.Ordinal) || error.Code.Contains("UserName", StringComparison.Ordinal)
                ? nameof(RegisterRequest.Email)
                : string.Empty;

            ModelState.AddModelError(field, error.Description);
        }
    }

    private ObjectResult InvalidCredentials() => Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
    #endregion
}
