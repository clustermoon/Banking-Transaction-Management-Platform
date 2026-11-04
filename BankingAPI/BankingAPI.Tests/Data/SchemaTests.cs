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

    #region FACT? - Database Rejects A Negative Balance
    [Fact]
    public async Task DatabaseRejectsANegativeBalance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Balance = -0.01 WHERE Id = {account.Id}", cancellationToken));

        Assert.Contains("CK_Accounts_Balance_NonNegative", error.Message, StringComparison.Ordinal);
    }
    #endregion

    #region FACT? - Database Rejects A Closed Account That Holds Money
    [Fact]
    public async Task DatabaseRejectsAClosedAccountThatHoldsMoney()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // Raw SQL stands in for a deposit that lands just before a close is saved.
        await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Balance = 25.00 WHERE Id = {account.Id}", cancellationToken);

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Status = 'Closed' WHERE Id = {account.Id}", cancellationToken));

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
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO LedgerEntries (AccountId, Type, Amount, BalanceAfter, Description, OccurredAt)
            VALUES ({account.Id}, 'Deposit', 10.00, 10.00, 'Test deposit', SYSDATETIMEOFFSET())
            """,
            cancellationToken);

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlRawAsync(
            tamperSql, [new SqlParameter("@accountId", account.Id)], cancellationToken));

        Assert.Equal(51000, error.Number);
    }
    #endregion

    #region THEORY? - Ledger Entries Must Have The Shape Of Their Type
    [Theory]
    [InlineData("TransferOut", false)]
    [InlineData("Deposit", true)]
    public async Task LedgerEntriesMustHaveTheShapeOfTheirType(string type, bool includeTransferFields)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var counterparty = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // A transfer entry missing its transfer fields, or a deposit carrying them.
        Guid? transferId = includeTransferFields ? Guid.NewGuid() : null;
        Guid? counterpartyId = includeTransferFields ? counterparty.Id : null;

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO LedgerEntries (AccountId, Type, Amount, BalanceAfter, Description, OccurredAt, TransferId, CounterpartyAccountId)
            VALUES ({account.Id}, {type}, 10.00, 10.00, 'Test', SYSDATETIMEOFFSET(), {transferId}, {counterpartyId})
            """,
            cancellationToken));

        Assert.Contains("CK_LedgerEntries_TransferShape", error.Message, StringComparison.Ordinal);
    }
    #endregion

    #region THEORY? - Role Is Seeded By Migration
    [Theory]
    [InlineData(Roles.Customer)]
    [InlineData(Roles.Admin)]
    public async Task RoleIsSeededByMigration(string role)
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Assert.True(await roleManager.RoleExistsAsync(role));
    }
    #endregion

    #region THEORY? - Audit Entries Cannot Be Rewritten
    [Theory]
    [InlineData("UPDATE AuditEntries SET Reason = 'Rewritten' WHERE AccountId = @accountId")]
    [InlineData("DELETE FROM AuditEntries WHERE AccountId = @accountId")]
    public async Task AuditEntriesCannotBeRewritten(string tamperSql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // The account's owner stands in for an actor; real entries come from the admin service.
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO AuditEntries (OccurredAt, ActorUserId, AccountId, FromStatus, ToStatus, Reason)
            VALUES (SYSDATETIMEOFFSET(), {account.OwnerId}, {account.Id}, 'Active', 'Frozen', 'Test entry')
            """, cancellationToken);

        var error = await Assert.ThrowsAsync<SqlException>(() => dbContext.Database.ExecuteSqlRawAsync(tamperSql, [new SqlParameter("@accountId", account.Id)], cancellationToken));

        Assert.Equal(51000, error.Number);
    }
    #endregion

}
