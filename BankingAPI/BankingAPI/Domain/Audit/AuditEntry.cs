using BankingAPI.Domain.Accounts;

namespace BankingAPI.Domain.Audit;

/// <summary>
/// An administrator's change to an account's status: who made it, when, the status before and
/// after, and why. Written in the same transaction as the change itself, so a change and its
/// entry exist together or not at all. A database trigger makes entries append-only.
/// </summary>
public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public long Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid AccountId { get; private set; }

    public AccountStatus FromStatus { get; private set; }

    public AccountStatus ToStatus { get; private set; }

    public string Reason { get; private set; } = string.Empty;


    internal static AuditEntry Record(Guid actorUserId, Guid accountId, AccountStatus fromStatus, AccountStatus toStatus, string reason, DateTimeOffset occurredAt) => new()
    {
        ActorUserId = actorUserId,
        AccountId = accountId,
        FromStatus = fromStatus,
        ToStatus = toStatus,
        Reason = reason,
        OccurredAt = occurredAt,
    };
}
