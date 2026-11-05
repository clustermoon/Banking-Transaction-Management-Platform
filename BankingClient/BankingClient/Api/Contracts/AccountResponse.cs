namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for an account response, representing the details of a bank account, including its unique identifier, account number, name, type, status, balance, and the date it was opened. This record is used to encapsulate the necessary information returned by the banking API when retrieving account details.
/// </summary>
/// <param name="Id"></param>
/// <param name="AccountNumber"></param>
/// <param name="Name"></param>
/// <param name="Type"></param>
/// <param name="Status"></param>
/// <param name="Balance"></param>
/// <param name="OpenedAt"></param>
public sealed record AccountResponse(Guid Id, string AccountNumber, string Name, AccountType Type, AccountStatus Status, decimal Balance, DateTimeOffset OpenedAt)
{
    public string MaskedNumber => $"•••• {AccountNumber[^4..]}";
}
