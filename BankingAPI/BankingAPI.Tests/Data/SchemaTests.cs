using BankingAPI.Data;
using BankingAPI.Identity;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Data;

public sealed class SchemaTests(BankingApiFactory factory)
{
    #region FACT? - Model Has No Changes Missing From Migrations
    [Fact]
    public void ModelHasNoChangesMissingFromMigrations()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        Assert.False(dbContext.Database.HasPendingModelChanges());
    }
    #endregion

    #region FACT? - Customer Role is Seeded By Migration
    [Fact]
    public async Task CustomerRoleIsSeededByMigration()
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Assert.True(await roleManager.RoleExistsAsync(Roles.Customer));
    }
    #endregion

    #region FACT? - Database Rejects A Negative Balance
    [Fact]
    public async Task DatabaseRejectsANegativeBalance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Balance = -0.01 WHERE Id = {accountId}", cancellationToken));

        Assert.Contains("CK_Accounts_Balance_NonNegative", error.Message, StringComparison.Ordinal);
    }
    #endregion

    #region FACT? - Database Rejects A Closed Account That Holds Money
    [Fact]
    public async Task DatabaseRejectsAClosedAccountThatHoldsMoney()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // Raw SQL stands in for a deposit that lands just before a close is saved.
        await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Balance = 25.00 WHERE Id = {accountId}", cancellationToken);

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Status = 'Closed' WHERE Id = {accountId}", cancellationToken));

        Assert.Contains("CK_Accounts_Closed_ZeroBalance", error.Message, StringComparison.Ordinal);
    }
    #endregion

    #region THEORY? - Ledger Entries Cannot Be Rewritten
    [Theory]
    [InlineData("UPDATE LedgerEntries SET Amount = 1 WHERE AccountId = @accountId")]
    [InlineData("DELETE FROM LedgerEntries WHERE AccountId = @accountId")]
    public async Task LedgerEntriesCannotBeRewritten(string tamperSql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO LedgerEntries (AccountId, Type, Amount, BalanceAfter, Description, OccurredAt)
            VALUES ({accountId}, 'Deposit', 10.00, 10.00, 'Test deposit', SYSDATETIMEOFFSET())
            """,
            cancellationToken);

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlRawAsync(
            tamperSql, [new SqlParameter("@accountId", accountId)], cancellationToken));

        Assert.Equal(51000, error.Number);
    }
    #endregion

}
