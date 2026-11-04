namespace BankingAPI.Domain.Ledger;

public sealed record LedgerOperationResult(LedgerEntry Entry, bool Replayed);
