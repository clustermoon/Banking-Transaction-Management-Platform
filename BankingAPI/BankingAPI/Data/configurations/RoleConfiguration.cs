using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingAPI.Data.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    // We need a fixed value to ensure it is deterministic otherwise every build will see a model change
    private static readonly Guid CustomerRoleId = new("6f1c2a4e-3b7d-4c8a-9e21-5d0f8b3a7c11");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder) =>
        builder.HasData(new IdentityRole<Guid>
        {
            Id = CustomerRoleId,
            Name = Roles.Customer,
            NormalizedName = Roles.Customer.ToUpperInvariant(),
            ConcurrencyStamp = "b8e5f0a2-1c4d-4e6f-8a9b-0c1d2e3f4a5b",
        });
}
