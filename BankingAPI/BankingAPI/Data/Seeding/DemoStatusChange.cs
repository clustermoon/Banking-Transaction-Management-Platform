using BankingAPI.Domain.Accounts;

namespace BankingAPI.Data.Seeding;

public sealed record DemoStatusChange(string AccountNumber, int DaysAgo, AccountStatus ToStatus, string Reason);
