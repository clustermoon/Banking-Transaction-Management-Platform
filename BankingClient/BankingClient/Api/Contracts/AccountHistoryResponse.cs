namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for account history responses, representing the historical data of a bank account over a specified date range, including the opening and closing balances and a list of ledger entries. This record is used to encapsulate the necessary information returned by the banking API when retrieving account history.
/// </summary>
/// <param name="From"></param>
/// <param name="To"></param>
/// <param name="OpeningBalance"></param>
/// <param name="ClosingBalance"></param>
/// <param name="Entries"></param>
public sealed record AccountHistoryResponse(DateOnly From, DateOnly To, decimal OpeningBalance, decimal ClosingBalance, IReadOnlyList<LedgerEntryResponse> Entries);
