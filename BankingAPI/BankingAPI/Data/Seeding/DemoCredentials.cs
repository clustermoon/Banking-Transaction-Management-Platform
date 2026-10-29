namespace BankingAPI.Data.Seeding;

/// <summary>
/// Public demo credentials, deliberately published in the README and on the client's login
/// page so reviewers can sign in immediately. Safe only because seeding is off by default and
/// refused in Production. Emails use the reserved .test domain, which can never receive mail.
/// </summary>
public static class DemoCredentials
{
    public const string CustomerPassword = "Avenor-Demo-2026";

    public static readonly IReadOnlyList<DemoUser> Customers =
    [
        // Everyday customer: accounts, deposits, and transfers.
        new("alice@avenor.test", "Alice Morgan"),

        // Isolation check: Ben must never be able to see Alice's accounts.
        new("ben@avenor.test", "Ben Carter"),

        // Edge cases: low balance, frozen account, closed account (given to her in Phase 4).
        new("cara@avenor.test", "Cara Lopez"),
    ];
}
