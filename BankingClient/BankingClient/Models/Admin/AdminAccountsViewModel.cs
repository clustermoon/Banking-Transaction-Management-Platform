using BankingClient.Api.Contracts;

namespace BankingClient.Models.Admin;

public sealed record AdminAccountsViewModel(IReadOnlyList<AdminAccountResponse> Accounts)
{
    public int ActiveCount => Accounts.Count(account => account.Status == AccountStatus.Active);

    public int FrozenCount => Accounts.Count(account => account.Status == AccountStatus.Frozen);

    public int ClosedCount => Accounts.Count(account => account.Status == AccountStatus.Closed);
}
