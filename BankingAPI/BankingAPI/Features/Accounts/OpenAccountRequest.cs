using BankingAPI.Domain.Accounts;

namespace BankingAPI.Features.Accounts;

public sealed record OpenAccountRequest(AccountType Type, string Name);
