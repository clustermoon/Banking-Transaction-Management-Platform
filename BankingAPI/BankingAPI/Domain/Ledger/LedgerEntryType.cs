namespace BankingAPI.Domain.Ledger;

/// <summary>
/// The direction of a ledger entry. Phase 5 adds the transfer types.
/// </summary>
public enum LedgerEntryType
{
    Deposit,
    Withdrawal,
    TransferOut,
    TransferIn
}
