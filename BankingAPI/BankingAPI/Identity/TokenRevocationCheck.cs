using BankingAPI.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Identity;

/// <summary>
/// Runs after a token's signature and lifetime pass. A token is rejected if its user no longer
/// exists, is locked out, or has a different security stamp, which logout and password
/// changes rotate. This makes revocation immediate at the cost of one lookup per request.
/// </summary>
internal static class TokenRevocationCheck
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = context.Principal!;

        var user = await userManager.GetUserAsync(principal);
        var tokenStamp = principal.FindFirst(userManager.Options.ClaimsIdentity.SecurityStampClaimType)?.Value;

        if (user is null
            || !string.Equals(tokenStamp, user.SecurityStamp, StringComparison.Ordinal)
            || await userManager.IsLockedOutAsync(user))
        {
            context.Fail("The access token has been revoked.");
        }
    }
}
