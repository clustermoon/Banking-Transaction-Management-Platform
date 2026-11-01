using BankingAPI.Domain.Ledger;

namespace BankingAPI.Data.Seeding;

/// <summary>
/// A historical money movement, dated relative to the day the demo is seeded.
/// </summary>
public sealed record DemoTransaction(string AccountNumber, int DaysAgo, LedgerEntryType Type, decimal Amount, string Description);
