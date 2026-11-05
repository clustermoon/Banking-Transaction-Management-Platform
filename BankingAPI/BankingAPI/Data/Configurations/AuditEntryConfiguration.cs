using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Audit;
using BankingAPI.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingAPI.Data.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable(table =>
        {
            table.HasTrigger("TR_AuditEntries_AppendOnly");

            table.HasCheckConstraint("CK_AuditEntries_StatusChanges", "[FromStatus] <> [ToStatus]");
        });

        builder.Property(entry => entry.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Reason).HasMaxLength(200);

        builder.HasIndex(entry => new { entry.AccountId, entry.Id });

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entry => entry.ActorUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>().WithMany().HasForeignKey(entry => entry.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
