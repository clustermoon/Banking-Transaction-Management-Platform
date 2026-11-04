namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for opening a new bank account, including the account type and name. This record is used to encapsulate the necessary information for initiating an account opening operation through the banking API.
/// </summary>
/// <param name="Type"></param>
/// <param name="Name"></param>
public sealed record OpenAccountRequest(AccountType Type, string Name);
