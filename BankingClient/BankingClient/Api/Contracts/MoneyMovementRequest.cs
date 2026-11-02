namespace BankingClient.Api.Contracts;

public sealed record MoneyMovementRequest(decimal Amount, string? Description);
