namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for an admin account response, representing detailed information about a bank account from an administrative perspective, including the account's unique identifier, account number, name, type, status, balance, opening date, and the owner's name and email. This record is used to encapsulate the necessary information returned by the banking API when retrieving account details for administrative purposes.
/// </summary>
/// <param name="Id"></param>
/// <param name="AccountNumber"></param>
/// <param name="Name"></param>
/// <param name="Type"></param>
/// <param name="Status"></param>
/// <param name="Balance"></param>
/// <param name="OpenedAt"></param>
/// <param name="OwnerName"></param>
/// <param name="OwnerEmail"></param>
public sealed record AdminAccountResponse(Guid Id, string AccountNumber, string Name, AccountType Type, AccountStatus Status, decimal Balance, DateTimeOffset OpenedAt, string OwnerName, string OwnerEmail)
{
    public string MaskedNumber => $"•••• {AccountNumber[^4..]}";
}
