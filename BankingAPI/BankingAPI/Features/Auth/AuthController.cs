using BankingAPI.Data;
using BankingAPI.Identity;
using BankingAPI.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AccessTokenIssuer tokenIssuer,
    BankingDbContext dbContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
        };

        // Creating the user and assigning the role are two saves; one transaction
        // keeps a failure from leaving a user with no role.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            AddIdentityErrors(created);
            return ValidationProblem(ModelState);
        }

        var roleAssigned = await userManager.AddToRoleAsync(user, Roles.Customer);
        if (!roleAssigned.Succeeded)
        {
            throw new InvalidOperationException($"Assigning the {Roles.Customer} role to a new user failed.");
        }

        await transaction.CommitAsync(cancellationToken);

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
