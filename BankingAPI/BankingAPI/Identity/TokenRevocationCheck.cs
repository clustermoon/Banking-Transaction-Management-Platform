using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Identity;

internal static class TokenRevocationCheck
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var principal = context.Principal!;

        var user = await userManager.GetUserAsync(principal);
        var tokenStamp = principal.FindFirst(userManager.Options.ClaimsIdentity.SecurityStampClaimType)?.Value;

        if (user is null || !string.Equals(tokenStamp, user.SecurityStamp, StringComparison.Ordinal) || await userManager.IsLockedOutAsync(user))
        {
            context.Fail("The access token has been revoked.");
        }
    }
}
