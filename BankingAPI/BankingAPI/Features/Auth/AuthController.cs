using BankingAPI.Data;
using BankingAPI.Identity;
using BankingAPI.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    BankingDbContext dbContext) : ControllerBase
{
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
}
