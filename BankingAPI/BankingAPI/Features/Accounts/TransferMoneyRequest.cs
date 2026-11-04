namespace BankingAPI.Features.Accounts;

public sealed record TransferMoneyRequest(string ToAccountNumber, decimal Amount, string? Note);
