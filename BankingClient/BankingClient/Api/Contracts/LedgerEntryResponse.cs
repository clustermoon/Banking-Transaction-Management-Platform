namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for a ledger entry response, representing a single entry in the account ledger, including details such as the entry ID, timestamp, type of transaction, description, signed amount, and the balance after the transaction. This record is used to encapsulate the necessary information returned by the banking API when retrieving ledger entries for an account.
/// </summary>
/// <param name="Id"></param>
/// <param name="OccurredAt"></param>
/// <param name="Type"></param>
/// <param name="Description"></param>
/// <param name="SignedAmount"></param>
/// <param name="BalanceAfter"></param>
public sealed record LedgerEntryResponse(long Id, DateTimeOffset OccurredAt, LedgerEntryType Type, string Description, decimal SignedAmount, decimal BalanceAfter);
