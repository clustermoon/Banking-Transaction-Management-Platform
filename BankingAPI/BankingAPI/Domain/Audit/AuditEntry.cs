using BankingAPI.Domain.Accounts;

namespace BankingAPI.Domain.Audit;

/// <summary>
/// An administrator's change to an account's status: who made it, when, the status before and
/// after, and why. Written in the same transaction as the change itself, so a change and its
/// entry exist together or not at all. A database trigger makes entries append-only.
/// </summary>
public sealed class AuditEntry
{
    // For EF Core. Entries are written by the account administration service.
    private AuditEntry()
    {
    }

    /// <summary>Database-generated; a higher Id is always a later entry.</summary>
    public long Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid AccountId { get; private set; }

    /// <summary>
    /// The action is the transition itself (Active → Frozen is a freeze), so it isn't stored
    /// separately where it could disagree with these two.
    /// </summary>
    public AccountStatus FromStatus { get; private set; }

    public AccountStatus ToStatus { get; private set; }

    /// <summary>Required: an audit trail records why, not only what.</summary>
    public string Reason { get; private set; } = string.Empty;
}
