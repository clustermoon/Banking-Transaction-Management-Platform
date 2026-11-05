namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for transferring money between bank accounts, including the target account number, transfer amount, and an optional note. This record is used to encapsulate the necessary information for initiating a transfer operation through the banking API.
/// </summary>
/// <param name="ToAccountNumber"></param>
/// <param name="Amount"></param>
/// <param name="Note"></param>
public sealed record TransferMoneyRequest(string ToAccountNumber, decimal Amount, string? Note);
