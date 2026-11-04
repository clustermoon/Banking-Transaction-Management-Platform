namespace BankingClient.Api.Contracts;

public sealed record AuditEntryResponse(long Id, DateTimeOffset OccurredAt, string ActorName, Guid AccountId, string AccountNumber, string AccountName, string OwnerName, AccountStatus FromStatus, AccountStatus ToStatus, string Reason)
{
    public string MaskedNumber => $"•••• {AccountNumber[^4..]}";
}
