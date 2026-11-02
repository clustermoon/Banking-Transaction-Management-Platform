using BankingClient.Api.Contracts;

namespace BankingClient.Models.Dashboard;

public sealed record DashboardViewModel(IReadOnlyList<AccountResponse> Accounts)
{
    /// <summary>
    /// Money across accounts that aren't closed; closed accounts always hold zero.
    /// </summary>
    public decimal TotalBalance => Accounts.Where(account => account.Status != AccountStatus.Closed).Sum(account => account.Balance);

    public int OpenAccounts => Accounts.Count(account => account.Status != AccountStatus.Closed);

    public int ActiveAccounts => Accounts.Count(account => account.Status == AccountStatus.Active);
}
