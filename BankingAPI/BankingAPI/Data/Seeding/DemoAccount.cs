using BankingAPI.Domain.Accounts;

namespace BankingAPI.Data.Seeding;

/// <summary>
/// A demo account, and the status it ends in once its history is recorded.
/// </summary>
public sealed record DemoAccount(string OwnerEmail, string AccountNumber, AccountType Type, string Name, int OpenedDaysAgo, AccountStatus Status);
