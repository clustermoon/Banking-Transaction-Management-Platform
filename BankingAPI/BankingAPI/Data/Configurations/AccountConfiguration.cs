using BankingAPI.Domain.Accounts;
using BankingAPI.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingAPI.Data.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Accounts_Balance_NonNegative", "[Balance] >= 0");
            table.HasCheckConstraint("CK_Accounts_Closed_ZeroBalance", "[Status] <> 'Closed' OR [Balance] = 0");
        });

        builder.Property(account => account.Balance).HasPrecision(18, 2);

        builder.Property(account => account.AccountNumber).HasMaxLength(AccountNumberGenerator.Length).IsFixedLength().IsUnicode(false);
        builder.HasIndex(account => account.AccountNumber).IsUnique();

        builder.Property(account => account.Name).HasMaxLength(50);

        builder.Property(account => account.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(account => account.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}
