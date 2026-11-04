using BankingClient.Api;
using BankingClient.Api.Contracts;

namespace BankingClient.Models.BankAccounts;

/// <summary>
/// Provides data for the account details view, including the account information, transaction history, and date range for the displayed history.
/// </summary>
public sealed class AccountDetailsViewModel
{
    public required AccountResponse Account { get; init; }

    public AccountHistoryResponse? History { get; init; }

    public ApiProblem? HistoryProblem { get; init; }

    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }
}
