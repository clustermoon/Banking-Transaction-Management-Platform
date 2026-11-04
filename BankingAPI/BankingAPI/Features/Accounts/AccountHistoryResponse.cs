namespace BankingAPI.Features.Accounts;

public sealed record AccountHistoryResponse(DateOnly From, DateOnly To, decimal OpeningBalance, decimal ClosingBalance, IReadOnlyList<LedgerEntryResponse> Entries);
