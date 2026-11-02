namespace BankingAPI.Features.Accounts;

/// <summary>A deposit or withdrawal. The idempotency key travels in the Idempotency-Key header.</summary>
public sealed record MoneyMovementRequest(decimal Amount, string? Description);
