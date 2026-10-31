using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BankingAPI.Identity;

/// <summary>
/// Issues HS256 access tokens. Times come from <see cref="TimeProvider"/> so tests can issue
/// tokens in the past and prove expiry is enforced.
/// </summary>
public sealed class AccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken Issue(ApplicationUser user, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.Add(JwtOptions.Lifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email!,
                [BankingClaimTypes.SecurityStamp] = user.SecurityStamp!,
                [BankingClaimTypes.Role] = roles.ToArray(),
            },
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }
}
