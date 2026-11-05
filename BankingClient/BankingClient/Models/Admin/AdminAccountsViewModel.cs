using BankingClient.Api.Contracts;

namespace BankingClient.Models.Admin;

/// <summary>
/// Provides data for the admin accounts view, including a list of all accounts and counts of accounts by status.
/// </summary>
/// <param name="Accounts"></param>
public sealed record AdminAccountsViewModel(IReadOnlyList<AdminAccountResponse> Accounts)
{
    public int ActiveCount => Accounts.Count(account => account.Status == AccountStatus.Active);

    public int FrozenCount => Accounts.Count(account => account.Status == AccountStatus.Frozen);

    public int ClosedCount => Accounts.Count(account => account.Status == AccountStatus.Closed);
}
