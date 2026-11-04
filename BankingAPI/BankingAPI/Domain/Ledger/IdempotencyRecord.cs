namespace BankingAPI.Domain.Ledger;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord() { }

    #region Idempotency Properties
    public Guid UserId { get; private set; }

    public Guid Key { get; private set; }

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
