using BankingAPI.Domain.Ledger;

namespace BankingAPI.Data.Seeding;

/// <summary>
/// Dated money movement dated to the day the demo is seeded. A transfer is written once.
/// </summary>
/// <param name="AccountNumber"></param>
/// <param name="DaysAgo"></param>
/// <param name="Type"></param>
/// <param name="Amount"></param>
/// <param name="Description"></param>
/// <param name="ToAccountNumber"></param>
public sealed record DemoTransaction(string AccountNumber, int DaysAgo, LedgerEntryType Type, decimal Amount, string Description, string? ToAccountNumber = null);
