using BankingAPI.Domain.Accounts;

namespace BankingAPI.Features.Accounts;

public sealed record AccountResponse(Guid Id, string AccountNumber, string Name, AccountType Type, AccountStatus Status, decimal Balance, DateTimeOffset OpenedAt)
{
    public static AccountResponse From(Account account) => new(account.Id, account.AccountNumber, account.Name, account.Type, account.Status, account.Balance, account.OpenedAt);
}
