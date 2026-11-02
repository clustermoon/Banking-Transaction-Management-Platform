namespace BankingAPI.Domain.Ledger;

/// <summary>
/// The result of a completed money operation, keyed by the idempotency key the client sent
/// with it. A retry with the same key and the same request returns the original result
/// instead of moving money again; the same key with a different request is refused.
/// </summary>
public sealed class IdempotencyRecord
{
    // For EF Core. Records are written by the ledger service.
    private IdempotencyRecord() { }

    #region Idempotency Properties
    public Guid UserId { get; private set; }

    public Guid Key { get; private set; }

    /// <summary>
    /// SHA-256 of the request, so a reused key with a different request is detected.
    /// </summary>
    public string RequestHash { get; private set; } = string.Empty;

    public long LedgerEntryId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    #endregion

    #region Idempotency Methods
    internal static IdempotencyRecord Create(
    Guid userId, Guid key, string requestHash, long ledgerEntryId, DateTimeOffset createdAt) => new()
    {
        UserId = userId,
        Key = key,
        RequestHash = requestHash,
        LedgerEntryId = ledgerEntryId,
        CreatedAt = createdAt,
    };
    #endregion

}
