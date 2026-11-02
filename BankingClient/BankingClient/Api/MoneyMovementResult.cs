using BankingClient.Api.Contracts;

namespace BankingClient.Api;

/// <summary>The recorded ledger entry, and whether the API recognized the request as a retry.</summary>
public sealed record MoneyMovementResult(LedgerEntryResponse Entry, bool Replayed);
