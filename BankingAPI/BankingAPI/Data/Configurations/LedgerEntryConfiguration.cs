using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingAPI.Data.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable(table =>
        {
            table.HasTrigger("TR_LedgerEntries_AppendOnly");

            table.HasCheckConstraint("CK_LedgerEntries_Amount_Positive", "[Amount] > 0");
            table.HasCheckConstraint("CK_LedgerEntries_BalanceAfter_NonNegative", "[BalanceAfter] >= 0");

            table.HasCheckConstraint(
                "CK_LedgerEntries_TransferShape",
                """
                ([Type] IN ('TransferOut', 'TransferIn') AND [TransferId] IS NOT NULL AND [CounterpartyAccountId] IS NOT NULL
                    AND [CounterpartyAccountId] <> [AccountId])
                OR ([Type] IN ('Deposit', 'Withdrawal') AND [TransferId] IS NULL AND [CounterpartyAccountId] IS NULL)
                """);
        });

        builder.Property(entry => entry.Amount).HasPrecision(18, 2);
        builder.Property(entry => entry.BalanceAfter).HasPrecision(18, 2);
        builder.Property(entry => entry.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Description).HasMaxLength(100);

        builder.HasIndex(entry => new { entry.AccountId, entry.Id });
        builder.HasIndex(entry => entry.TransferId);

        builder.HasOne<Account>().WithMany().HasForeignKey(entry => entry.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(entry => entry.CounterpartyAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
