using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingAPI.Data.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    private static readonly Guid CustomerRoleId = new("6f1c2a4e-3b7d-4c8a-9e21-5d0f8b3a7c11");
    private static readonly Guid AdminRoleId = new("9a4d7e2b-6c1f-4b83-a5d0-2e8f1c7b4a39");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder) => builder.HasData(new IdentityRole<Guid>
    {
        Id = CustomerRoleId,
        Name = Roles.Customer,
        NormalizedName = Roles.Customer.ToUpperInvariant(),
        ConcurrencyStamp = "b8e5f0a2-1c4d-4e6f-8a9b-0c1d2e3f4a5b",
    },
    new IdentityRole<Guid>
    {
        Id = AdminRoleId,
        Name = Roles.Admin,
        NormalizedName = Roles.Admin.ToUpperInvariant(),
        ConcurrencyStamp = "d3f7a1c9-5e2b-4d68-b0a4-7c9e1f3b5d27",
    });
}
