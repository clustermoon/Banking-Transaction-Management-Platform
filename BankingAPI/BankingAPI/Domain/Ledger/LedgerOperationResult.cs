namespace BankingAPI.Domain.Ledger;

/// <summary>
/// The ledger entry a money operation produced. <see cref="Replayed"/> is true when the
/// request was a retry and this is the original result, with no money moved this time.
/// </summary>
public sealed record LedgerOperationResult(LedgerEntry Entry, bool Replayed);
