using BankingClient.Api.Contracts;

namespace BankingClient.Models;

public static class LedgerEntryTypeNames
{
    public static string ToDisplayName(this LedgerEntryType type) => type switch
    {
        LedgerEntryType.Deposit => "Deposit",
        LedgerEntryType.Withdrawal => "Withdrawal",
        LedgerEntryType.TransferOut => "Transfer out",
        LedgerEntryType.TransferIn => "Transfer in",
        _ => type.ToString(),
    };
}
