namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for requesting a money movement operation, such as a deposit or withdrawal, including the amount and an optional description. This record is used to encapsulate the necessary information for initiating a money movement operation through the banking API.
/// </summary>
/// <param name="Amount"></param>
/// <param name="Description"></param>
public sealed record MoneyMovementRequest(decimal Amount, string? Description);
