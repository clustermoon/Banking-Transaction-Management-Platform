using BankingAPI.Data;
using BankingAPI.Identity;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Data;

public sealed class SchemaTests(BankingApiFactory factory)
{
    [Fact]
    public void ModelHasNoChangesMissingFromMigrations()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        Assert.False(dbContext.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task CustomerRoleIsSeededByMigration()
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Assert.True(await roleManager.RoleExistsAsync(Roles.Customer));
    }
}
