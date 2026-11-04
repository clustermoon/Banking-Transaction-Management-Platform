namespace BankingAPI.Domain.Ledger;

public sealed record TransferRequest(Guid UserId, Guid FromAccountId, string ToAccountNumber, decimal Amount, string? Note, Guid IdempotencyKey);
