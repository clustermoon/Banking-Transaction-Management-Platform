namespace BankingClient.Api.Contracts;

public sealed record AccountResponse(Guid Id, string AccountNumber, string Name, AccountType Type, AccountStatus Status, decimal Balance, DateTimeOffset OpenedAt)
{
    /// <summary>Only the last four digits, for lists and headers; the full number appears in account details.</summary>
    public string MaskedNumber => $"•••• {AccountNumber[^4..]}";
}
