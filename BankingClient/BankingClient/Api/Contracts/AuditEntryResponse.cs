namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for an audit entry response, representing a single entry in the account audit log, including details such as the entry ID, timestamp, actor name, account information, status changes, and the reason for the change. This record is used to encapsulate the necessary information returned by the banking API when retrieving audit entries for an account.
/// </summary>
/// <param name="Id"></param>
/// <param name="OccurredAt"></param>
/// <param name="ActorName"></param>
/// <param name="AccountId"></param>
/// <param name="AccountNumber"></param>
/// <param name="AccountName"></param>
/// <param name="OwnerName"></param>
/// <param name="FromStatus"></param>
/// <param name="ToStatus"></param>
/// <param name="Reason"></param>
public sealed record AuditEntryResponse(long Id, DateTimeOffset OccurredAt, string ActorName, Guid AccountId, string AccountNumber, string AccountName, string OwnerName, AccountStatus FromStatus, AccountStatus ToStatus, string Reason)
{
    public string MaskedNumber => $"•••• {AccountNumber[^4..]}";
}
