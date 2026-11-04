using BankingAPI.Domain.Accounts;

namespace BankingAPI.Domain.Audit;

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
