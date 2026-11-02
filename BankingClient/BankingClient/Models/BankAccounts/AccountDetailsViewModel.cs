using BankingClient.Api;
using BankingClient.Api.Contracts;

namespace BankingClient.Models.BankAccounts;

public sealed class AccountDetailsViewModel
{
    public required AccountResponse Account { get; init; }

    public AccountHistoryResponse? History { get; init; }

    /// <summary>
    /// Set when the date range was invalid; the page shows it instead of the history.
    /// </summary>
    public ApiProblem? HistoryProblem { get; init; }

    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }
}
