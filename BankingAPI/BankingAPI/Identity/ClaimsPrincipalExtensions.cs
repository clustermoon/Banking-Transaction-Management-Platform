using System.Globalization;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace BankingAPI.Identity;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new InvalidOperationException("The signed-in user has no subject claim."), CultureInfo.InvariantCulture);
}
