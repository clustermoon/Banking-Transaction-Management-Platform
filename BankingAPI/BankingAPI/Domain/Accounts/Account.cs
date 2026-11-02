namespace BankingAPI.Domain.Accounts;

/// <summary>
/// A customer's checking or savings account. Accounts are never deleted; they're closed.
/// Status changes go through the lifecycle methods below. The balance is never changed
/// on this object: money moves only through the ledger service's atomic database updates,
/// which keep the balance equal to the account's ledger history.
/// </summary>
public sealed class Account
{
    #region Account Properties
    // For EF Core, which materializes accounts from the database.
    private Account() { }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string AccountNumber { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public AccountType Type { get; private set; }

    public AccountStatus Status { get; private set; }

    public decimal Balance { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public string MaskedAccountNumber => $"•••• {AccountNumber[^4..]}";

    #endregion

    #region Account Methods
    /// <summary>
    /// Every account starts active with a zero balance; money arrives only through deposits.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="accountNumber"></param>
    /// <param name="openedAt"></param>
    /// <returns></returns>
    public static Account Open(Guid ownerId, AccountType type, string name, string accountNumber, DateTimeOffset openedAt) => new()
    {
        OwnerId = ownerId,
        Type = type,
        Name = name.Trim(),
        AccountNumber = accountNumber,
        Status = AccountStatus.Active,
        Balance = 0m,
        OpenedAt = openedAt,
    };

    /// <summary>
    /// Freezing an account makes it visible but prevents any money from moving in or out. This
    /// </summary>
    /// <exception cref="BusinessRuleException"></exception>
    public void Freeze()
    {
        if (Status != AccountStatus.Active)
        {
            throw new BusinessRuleException("Only an active account can be frozen.");
        }

        Status = AccountStatus.Frozen;
    }

    /// <summary>
    /// Unfreezing an account returns it to active status, where it can move money again.
    /// </summary>
    /// <exception cref="BusinessRuleException"></exception>
    public void Unfreeze()
    {
        if (Status != AccountStatus.Frozen)
        {
            throw new BusinessRuleException("Only a frozen account can be unfrozen.");
        }

        Status = AccountStatus.Active;
    }

    /// <summary>
    /// Closed accounts are kept permanently for their history and can never be reopened.
    /// The balance check here gives a clear message. A database constraint enforces the same
    /// rule at save time, so a deposit that lands after this check can't leave a closed
    /// account holding money.
    /// </summary>
    /// <exception cref="BusinessRuleException"></exception>
    public void Close()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new BusinessRuleException("This account is already closed.");
        }

        if (Balance != 0m)
        {
            throw new BusinessRuleException("An account can only be closed once its balance is zero.");
        }

        Status = AccountStatus.Closed;
    }
    #endregion
}
