namespace BankingClient.Api.Contracts;

public sealed record TransferMoneyRequest(string ToAccountNumber, decimal Amount, string? Note);
