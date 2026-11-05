using BankingAPI.Domain.Accounts;

namespace BankingAPI.Features.Admin;

public sealed record AuditEntryResponse(long Id, DateTimeOffset OccurredAt, string ActorName, Guid AccountId, string AccountNumber, string AccountName, string OwnerName, AccountStatus FromStatus, AccountStatus ToStatus, string Reason);
