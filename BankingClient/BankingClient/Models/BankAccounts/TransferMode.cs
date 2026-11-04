namespace BankingClient.Models.BankAccounts;

/// <summary>
/// Provides transfer modes for bank account transfers, distinguishing between transfers to the customer's own accounts and transfers to another customer's account.
/// </summary>
public enum TransferMode
{
    OwnAccounts,
    AnotherCustomer,
}
