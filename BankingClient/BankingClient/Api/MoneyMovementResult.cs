using BankingClient.Api.Contracts;

namespace BankingClient.Api;

/// <summary>
/// A record that represents the result of a money movement operation, including the ledger entry created and whether the operation was replayed. This is used to provide feedback to the user after a deposit, withdrawal, or transfer operation.
/// </summary>
/// <param name="Entry"></param>
/// <param name="Replayed"></param>
public sealed record MoneyMovementResult(LedgerEntryResponse Entry, bool Replayed);
