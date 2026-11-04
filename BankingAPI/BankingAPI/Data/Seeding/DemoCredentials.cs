namespace BankingAPI.Data.Seeding;

public static class DemoCredentials
{
    // Customers
    public const string CustomerPassword = "Avenor-Demo-2026";
    public const string AliceEmail = "alice@avenor.test";
    public const string BenEmail = "ben@avenor.test";
    public const string CaraEmail = "cara@avenor.test";


    // Admin 
    public const string AdminEmail = "admin@avenor.test";
    public const string AdminPassword = "Avenor-Admin-2026";
    public const string AdminDisplayName = "Sam Rivera";

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
