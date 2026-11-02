namespace BankingAPI.Domain.Ledger;

// Positive amounts with a type, instead of signed amounts, make it easier to reason about the ledger. 
// A deposit is always positive, and a withdrawal is always positive. The type gives the direction. 
// This is also how the ledger is stored in the database. BalanceAfter is stored, not calculated,
// so that the ledger can be audited and reconciled even if the account's balance is wrong.

/// <summary>
/// One money movement on one account. Entries are append-only: a database trigger rejects
/// any update or delete, so history can be added to but never rewritten.
/// </summary>
public sealed class LedgerEntry
{
    // For EF Core. Entries are written by the ledger service.
    private LedgerEntry() { }

    #region Ledger Properties
    /// <summary>
    /// Database-generated insertion order. An account's entries are written while its row is
    /// locked, so within one account a higher Id is always a later entry.
    /// </summary>
    public long Id { get; private set; }

    public Guid AccountId { get; private set; }

    public LedgerEntryType Type { get; private set; }

    /// <summary>Always positive; <see cref="Type"/> gives the direction.</summary>
    public decimal Amount { get; private set; }

    /// <summary>The account's balance immediately after this entry, which drives running balances and statements.</summary>
    public decimal BalanceAfter { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }
    #endregion

    #region Ledger Methods
    /// <summary>
    /// The amount with its direction applied: positive for money in, negative for money out.
    /// </summary>
    public decimal SignedAmount => Type switch
    {
        LedgerEntryType.Deposit => Amount,
        LedgerEntryType.Withdrawal => -Amount,
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
    #endregion

}
