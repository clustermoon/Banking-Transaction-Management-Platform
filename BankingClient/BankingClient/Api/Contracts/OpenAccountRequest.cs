namespace BankingClient.Api.Contracts;

public sealed record OpenAccountRequest(AccountType Type, string Name);
