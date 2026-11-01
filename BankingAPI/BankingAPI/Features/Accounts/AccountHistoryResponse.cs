namespace BankingAPI.Features.Accounts;

/// <summary>
/// A statement: the balance at the start of the range, the entries within it (newest first),
/// and the balance at the end. Opening balance plus every entry's signed amount always equals
/// the closing balance.
/// </summary>
public sealed record AccountHistoryResponse(DateOnly From, DateOnly To, decimal OpeningBalance, decimal ClosingBalance, IReadOnlyList<LedgerEntryResponse> Entries);
