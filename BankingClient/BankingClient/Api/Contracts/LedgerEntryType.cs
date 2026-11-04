namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for ledger entry types, representing the different types of transactions that can occur in a bank account, such as deposits, withdrawals, and transfers. This enum is used to categorize and identify the nature of each ledger entry in the banking system.
/// </summary>
public enum LedgerEntryType
{
    Deposit,
    Withdrawal,
    TransferOut,
    TransferIn,
}
