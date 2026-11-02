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
            // Declared so EF Core avoids SQL that fails on tables with triggers.
            // The trigger itself is created in the AddAccountsAndLedger migration.
            table.HasTrigger("TR_LedgerEntries_AppendOnly");

            table.HasCheckConstraint("CK_LedgerEntries_Amount_Positive", "[Amount] > 0");
            table.HasCheckConstraint("CK_LedgerEntries_BalanceAfter_NonNegative", "[BalanceAfter] >= 0");
        });

        builder.Property(entry => entry.Amount).HasPrecision(18, 2);
        builder.Property(entry => entry.BalanceAfter).HasPrecision(18, 2);
        builder.Property(entry => entry.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Description).HasMaxLength(100);

        // History is read per account, newest first; this index serves exactly that query.
        builder.HasIndex(entry => new { entry.AccountId, entry.Id });

        builder.HasOne<Account>().WithMany().HasForeignKey(entry => entry.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
