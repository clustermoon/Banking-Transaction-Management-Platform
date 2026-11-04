namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for account statuses, representing the different states that a bank account can be in, such as active, frozen, or closed. This enum is used to categorize and identify the current status of each bank account in the banking system.
/// </summary>
public enum AccountStatus
{
    Active,
    Frozen,
    Closed,
}
