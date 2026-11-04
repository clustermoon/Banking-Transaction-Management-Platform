using BankingAPI.Domain.Accounts;

namespace BankingAPI.Features.Admin;

public sealed record AdminAccountResponse(Guid Id, string AccountNumber, string Name, AccountType Type, AccountStatus Status, decimal Balance, DateTimeOffset OpenedAt, string OwnerName, string OwnerEmail);
