using BankingAPI.Domain.Accounts;

namespace BankingAPI.Data.Seeding;

public sealed record DemoAccount(string OwnerEmail, string AccountNumber, AccountType Type, string Name, int OpenedDaysAgo);
