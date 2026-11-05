namespace BankingAPI.Features.Accounts;

public sealed record MoneyMovementRequest(decimal Amount, string? Description);
