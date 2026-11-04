using BankingAPI.Domain.Ledger;
using BankingAPI.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingAPI.Data.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.HasKey(record => new { record.UserId, record.Key });

        builder.Property(record => record.RequestHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(record => record.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LedgerEntry>().WithMany().HasForeignKey(record => record.LedgerEntryId).OnDelete(DeleteBehavior.Restrict);
    }
}
