using System.ComponentModel.DataAnnotations;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace BankingAPI.Identity;

/// <summary>
/// Access-token settings, validated at startup so a missing or weak signing key stops the API
/// from starting rather than surfacing as a runtime failure on the first login.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Short-lived by design: there are no refresh tokens, and revocation is per request.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>HS256 requires at least 256 bits of key material.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
