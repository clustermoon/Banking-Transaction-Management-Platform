namespace BankingAPI.Domain.Ledger;

/// <summary>
/// A money operation a user requests on one of their accounts. The client generates the
/// idempotency key once per intended operation and sends the same key on every retry.
/// </summary>
public sealed record LedgerRequest(Guid UserId, Guid AccountId, decimal Amount, string Description, Guid IdempotencyKey);
