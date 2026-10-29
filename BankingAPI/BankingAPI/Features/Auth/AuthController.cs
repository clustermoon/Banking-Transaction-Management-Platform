using BankingAPI.Identity;
using BankingAPI.Infrastructure;

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

        return StatusCode(
            StatusCodes.Status201Created,
            new RegisterResponse(user.Id, request.Email, user.DisplayName));
    }

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

        var token = tokenIssuer.Issue(user, await userManager.GetRolesAsync(user));
        return Ok(new LoginResponse(token.Value, token.ExpiresAt));
    }

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

        // Rotating the stamp revokes every token issued before now: TokenRevocationCheck
        // compares each request's token stamp against this value.
        var rotated = await userManager.UpdateSecurityStampAsync(user);
        if (!rotated.Succeeded)
        {
            throw new InvalidOperationException("Rotating the security stamp during logout failed.");
        }

        return NoContent();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            // UserName mirrors Email, so its duplicate error would only repeat the email one.
            if (error.Code == nameof(IdentityErrorDescriber.DuplicateUserName))
            {
                continue;
            }

            var field = error.Code.StartsWith("Password", StringComparison.Ordinal)
                ? nameof(RegisterRequest.Password)
                : error.Code.Contains("Email", StringComparison.Ordinal) || error.Code.Contains("UserName", StringComparison.Ordinal)
                    ? nameof(RegisterRequest.Email)
                    : string.Empty;

            ModelState.AddModelError(field, error.Description);
        }
    }

    // One message for an unknown email and a wrong password, so login alone can't
    // be used to test which emails are registered.
    private ObjectResult InvalidCredentials() =>
        Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
}
