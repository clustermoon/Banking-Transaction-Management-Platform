namespace BankingAPI.Data.Seeding;

internal sealed class SeedingClock : TimeProvider
{
    private DateTimeOffset utcNow;

    public void SetUtcNow(DateTimeOffset value) => utcNow = value;

    public override DateTimeOffset GetUtcNow() => utcNow;
}
