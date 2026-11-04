using BankingAPI.Domain.Ledger;

namespace BankingAPI.Features.Accounts;

public sealed record LedgerEntryResponse( long Id, DateTimeOffset OccurredAt, LedgerEntryType Type, string Description, decimal SignedAmount, decimal BalanceAfter)
{
    public static LedgerEntryResponse From(LedgerEntry entry) => new(entry.Id, entry.OccurredAt, entry.Type, entry.Description, entry.SignedAmount, entry.BalanceAfter);
}
