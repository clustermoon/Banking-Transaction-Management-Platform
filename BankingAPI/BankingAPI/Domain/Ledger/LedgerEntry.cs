namespace BankingAPI.Domain.Ledger;

public sealed class LedgerEntry
{
    private LedgerEntry() { }

    #region Ledger Properties
    public long Id { get; private set; }

    public Guid AccountId { get; private set; }

    public LedgerEntryType Type { get; private set; }

    public decimal Amount { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? TransferId { get; private set; }

    public Guid? CounterpartyAccountId { get; private set; }

    #endregion

    #region Ledger Methods
    public decimal SignedAmount => Type switch
    {
        LedgerEntryType.Deposit or LedgerEntryType.TransferIn => Amount,
        LedgerEntryType.Withdrawal or LedgerEntryType.TransferOut => -Amount,
        _ => throw new InvalidOperationException($"Unknown ledger entry type {Type}."),
    };

    internal static LedgerEntry Record(Guid accountId, LedgerEntryType type, decimal amount, decimal balanceAfter, string description, DateTimeOffset occurredAt) => new()
    {
        AccountId = accountId,
        Type = type,
        Amount = amount,
        BalanceAfter = balanceAfter,
        Description = description,
        OccurredAt = occurredAt,
    };

    internal static LedgerEntry RecordTransferLeg(Guid accountId, LedgerEntryType type, decimal amount, decimal balanceAfter, string description, DateTimeOffset occurredAt, Guid transferId, Guid counterpartyAccountId) => new()
    {
        AccountId = accountId,
        Type = type,
        Amount = amount,
        BalanceAfter = balanceAfter,
        Description = description,
        OccurredAt = occurredAt,
        TransferId = transferId,
        CounterpartyAccountId = counterpartyAccountId,
    };

    #endregion

}
