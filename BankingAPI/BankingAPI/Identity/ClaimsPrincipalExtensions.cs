using System.Globalization;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace BankingAPI.Identity;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's id, from the token's subject claim. Only meaningful on endpoints
    /// that require authentication, which is every endpoint unless it opts out.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new InvalidOperationException("The signed-in user has no subject claim."), CultureInfo.InvariantCulture);
}
