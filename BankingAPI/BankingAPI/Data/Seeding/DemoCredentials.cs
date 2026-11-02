namespace BankingAPI.Data.Seeding;

/// <summary>
/// Public demo credentials, deliberately published in the README and on the client's login
/// page so reviewers can sign in immediately. Safe only because seeding is off by default and
/// refused in Production. Emails use the reserved .test domain, which can never receive mail.
/// </summary>
public static class DemoCredentials
{
    public const string CustomerPassword = "Avenor-Demo-2026";
    public const string AliceEmail = "alice@avenor.test";
    public const string BenEmail = "ben@avenor.test";
    public const string CaraEmail = "cara@avenor.test";

    public static readonly IReadOnlyList<DemoUser> Customers =
    [
        // Everyday customer: accounts, deposits, and transfers.
        new(AliceEmail, "Alice Morgan"),

        // Isolation check: Ben must never be able to see Alice's accounts.
        new(BenEmail, "Ben Carter"),

        // Edge cases: a low balance, a frozen account, and a closed account.
        new(CaraEmail, "Cara Lopez"),
    ];
}
