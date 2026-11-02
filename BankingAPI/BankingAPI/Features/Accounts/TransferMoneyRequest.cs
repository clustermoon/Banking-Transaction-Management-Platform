namespace BankingAPI.Features.Accounts;

/// <summary>
/// A transfer from the account in the route to any active account, identified by its number.
/// The idempotency key travels in the Idempotency-Key header.
/// </summary>
public sealed record TransferMoneyRequest(string ToAccountNumber, decimal Amount, string? Note);
