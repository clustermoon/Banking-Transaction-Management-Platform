using BankingClient.Api.Contracts;

namespace BankingClient.Models.Dashboard;

/// <summary>
/// Provides data for the dashboard view, including a list of accounts and summary statistics.
/// </summary>
/// <param name="Accounts"></param>
public sealed record DashboardViewModel(IReadOnlyList<AccountResponse> Accounts)
{
    public decimal TotalBalance => Accounts.Where(account => account.Status != AccountStatus.Closed).Sum(account => account.Balance);

    public int OpenAccounts => Accounts.Count(account => account.Status != AccountStatus.Closed);

    public int ActiveAccounts => Accounts.Count(account => account.Status == AccountStatus.Active);
}
