namespace BankingAPI.Identity;

/// <summary>
/// Claim names this API issues beyond the registered JWT names (sub, email). Identity is
/// configured to read these same names, so the token and UserManager share one vocabulary.
/// </summary>
public static class BankingClaimTypes
{
    public const string Role = "role";
    public const string SecurityStamp = "security_stamp";
}
