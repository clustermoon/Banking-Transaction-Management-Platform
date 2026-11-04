namespace BankingClient.Api.Contracts;

public sealed record AdminAccountResponse(Guid Id, string AccountNumber, string Name, AccountType Type, AccountStatus Status, decimal Balance, DateTimeOffset OpenedAt, string OwnerName, string OwnerEmail)
{
    public string MaskedNumber => $"•••• {AccountNumber[^4..]}";
}
