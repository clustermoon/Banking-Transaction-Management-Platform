namespace BankingAPI.Data.Seeding;

/// <summary>
/// A clock the seeder sets to each historical moment, so demo history is recorded through the
/// real ledger service with realistic dates. Used only by the seeder; the API runs on the system clock.
/// </summary>
internal sealed class SeedingClock : TimeProvider
{
    private DateTimeOffset utcNow;

    public void SetUtcNow(DateTimeOffset value) => utcNow = value;

    public override DateTimeOffset GetUtcNow() => utcNow;
}
