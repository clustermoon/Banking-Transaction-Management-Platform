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
        new(AliceEmail, "Alice Morgan"),

        new(BenEmail, "Ben Carter"),

        new(CaraEmail, "Cara Lopez"),
    ];
}
