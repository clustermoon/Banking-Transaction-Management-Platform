using BankingAPI.Data;
using BankingAPI.Domain;
using BankingAPI.Domain.Accounts;
using BankingAPI.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Domain.Accounts;

public sealed class AccountTests(BankingApiFactory factory)
{
    private static Account OpenAccount() => Account.Open(Guid.NewGuid(), AccountType.Checking, "  Everyday Checking  ", "1234567890", DateTimeOffset.UtcNow);

    #region FACT? - Opened Account Is Active With Zero Balance
    [Fact]
    public void OpenedAccountIsActiveWithZeroBalance()
    {
        var account = OpenAccount();

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(0m, account.Balance);
        Assert.Equal("Everyday Checking", account.Name);
    }
    #endregion

    #region FACT? - Frozen Account Can Be Unfrozen
    [Fact]
    public void FrozenAccountCanBeUnfrozen()
    {
        var account = OpenAccount();

        account.Freeze();
        account.Unfreeze();

        Assert.Equal(AccountStatus.Active, account.Status);
    }
    #endregion

    #region FACT? - Only An Active Account Can Be Frozen
    [Fact]
    public void OnlyAnActiveAccountCanBeFrozen()
    {
        var account = OpenAccount();
        account.Freeze();

        Assert.Throws<BusinessRuleException>(account.Freeze);
    }
    #endregion

    #region FACT? - Closed Account Is Final
    [Fact]
    public void ClosedAccountIsFinal()
    {
        var account = OpenAccount();
        account.Close();

        Assert.Throws<BusinessRuleException>(account.Freeze);
        Assert.Throws<BusinessRuleException>(account.Unfreeze);
        Assert.Throws<BusinessRuleException>(account.Close);
    }
    #endregion

    #region FACT? - Account Holding Money Cannot Be Closed
    [Fact]
    public async Task AccountHoldingMoneyCannotBeClosed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var testAccount = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // A balance can only come from the database, which is how real accounts are loaded.
        await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Accounts SET Balance = 25.00 WHERE Id = {testAccount.Id}", cancellationToken);
        var account = await dbContext.Accounts.SingleAsync(candidate => candidate.Id == testAccount.Id, cancellationToken);

        var error = Assert.Throws<BusinessRuleException>(account.Close);
        Assert.Equal("An account can only be closed once its balance is zero.", error.Message);
    }
    #endregion

    #region FACT? - Concurrent Status Changes Cannot Both Succeed
    [Fact]
    public async Task ConcurrentStatusChangesCannotBothSucceed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);

        // Two admins load the same active account, each with their own context.
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstAdmin = firstScope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var secondAdmin = secondScope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var firstView = await firstAdmin.Accounts.SingleAsync(candidate => candidate.Id == account.Id, cancellationToken);
        var secondView = await secondAdmin.Accounts.SingleAsync(candidate => candidate.Id == account.Id, cancellationToken);

        // Both decide from "Active"; the first to save wins.
        firstView.Freeze();
        secondView.Close();
        await firstAdmin.SaveChangesAsync(cancellationToken);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondAdmin.SaveChangesAsync(cancellationToken));
    }
    #endregion
}
