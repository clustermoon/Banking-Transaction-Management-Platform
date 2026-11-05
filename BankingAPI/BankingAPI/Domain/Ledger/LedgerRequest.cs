namespace BankingAPI.Domain.Ledger;

public sealed record LedgerRequest(Guid UserId, Guid AccountId, decimal Amount, string Description, Guid IdempotencyKey);
