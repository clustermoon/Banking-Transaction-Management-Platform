namespace BankingAPI.Domain.Accounts;

/// <summary>
/// Active accounts move money. Frozen accounts are visible but can't move money.
/// Closed accounts are kept permanently for their history and can never be reopened.
/// </summary>
public enum AccountStatus
{
    Active,
    Frozen,
    Closed,
}
