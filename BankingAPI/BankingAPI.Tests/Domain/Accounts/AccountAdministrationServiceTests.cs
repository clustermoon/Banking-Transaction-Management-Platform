using BankingAPI.Data;
using BankingAPI.Domain;
using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Audit;
using BankingAPI.Domain.Ledger;
using BankingAPI.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Domain.Accounts;

public sealed class AccountAdministrationServiceTests(BankingApiFactory factory)
{
    #region FACT? - Freeze Records An Entry Matching The Change
    [Fact]
    public async Task FreezeRecordsAnEntryMatchingTheChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var admin = await DatabaseTestData.CreateUserAsync(factory, cancellationToken);

        var entry = await RunAsync(service => service.FreezeAsync(admin, account.Id, "  Suspected fraud  ", cancellationToken));

        Assert.Equal(AccountStatus.Frozen, await GetStatusAsync(account, cancellationToken));
        Assert.Equal(admin, entry.ActorUserId);
        Assert.Equal(AccountStatus.Active, entry.FromStatus);
        Assert.Equal(AccountStatus.Frozen, entry.ToStatus);
        Assert.Equal("Suspected fraud", entry.Reason);
        Assert.Single(await GetAuditEntriesAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Every Change In A Lifecycle Is Recorded In Order
    [Fact]
    public async Task EveryChangeInALifecycleIsRecordedInOrder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var admin = await DatabaseTestData.CreateUserAsync(factory, cancellationToken);

        await RunAsync(service => service.FreezeAsync(admin, account.Id, "Investigation opened", cancellationToken));
        await RunAsync(service => service.UnfreezeAsync(admin, account.Id, "Investigation cleared", cancellationToken));
        await RunAsync(service => service.CloseAsync(admin, account.Id, "Customer request", cancellationToken));

        var entries = await GetAuditEntriesAsync(account, cancellationToken);
        Assert.Equal(
            [
                (AccountStatus.Active, AccountStatus.Frozen),
                (AccountStatus.Frozen, AccountStatus.Active),
                (AccountStatus.Active, AccountStatus.Closed),
            ],
            entries.Select(entry => (entry.FromStatus, entry.ToStatus)));
    }
    #endregion

    #region FACT? - Refused Change Writes No Entry
    [Fact]
    public async Task RefusedChangeWritesNoEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var admin = await DatabaseTestData.CreateUserAsync(factory, cancellationToken);

        await Assert.ThrowsAsync<BusinessRuleException>(() => RunAsync(service => service.UnfreezeAsync(admin, account.Id, "Not needed", cancellationToken)));

        Assert.Equal(AccountStatus.Active, await GetStatusAsync(account, cancellationToken));
        Assert.Empty(await GetAuditEntriesAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Account Holding Money Cannot Be Closed and Nothing Is Recorded
    [Fact]
    public async Task AccountHoldingMoneyCannotBeClosedAndNothingIsRecorded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var admin = await DatabaseTestData.CreateUserAsync(factory, cancellationToken);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<LedgerService>().DepositAsync(new LedgerRequest(account.OwnerId, account.Id, 25m, "Test", Guid.NewGuid()), cancellationToken);
        }

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => RunAsync(service => service.CloseAsync(admin, account.Id, "Customer request", cancellationToken)));

        Assert.Equal("An account can only be closed once its balance is zero.", error.Message);
        Assert.Equal(AccountStatus.Active, await GetStatusAsync(account, cancellationToken));
        Assert.Empty(await GetAuditEntriesAsync(account, cancellationToken));
    }
    #endregion

    #region THEORY? - Every Change Needs A Reason
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EveryChangeNeedsAReason(string reason)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var admin = await DatabaseTestData.CreateUserAsync(factory, cancellationToken);

        await Assert.ThrowsAsync<BusinessRuleException>(() => RunAsync(service => service.FreezeAsync(admin, account.Id, reason, cancellationToken)));

        Assert.Equal(AccountStatus.Active, await GetStatusAsync(account, cancellationToken));
        Assert.Empty(await GetAuditEntriesAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Unknown Account Is Not Found
    [Fact]
    public async Task UnknownAccountIsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var admin = await DatabaseTestData.CreateUserAsync(factory, cancellationToken);

        await Assert.ThrowsAsync<AccountNotFoundException>(() => RunAsync(service => service.FreezeAsync(admin, Guid.NewGuid(), "Test", cancellationToken)));
    }
    #endregion

    #region Helpers
    private async Task<AuditEntry> RunAsync(Func<AccountAdministrationService, Task<AuditEntry>> operation)
    {
        using var scope = factory.Services.CreateScope();
        return await operation(scope.ServiceProvider.GetRequiredService<AccountAdministrationService>());
    }

    private async Task<AccountStatus> GetStatusAsync(TestAccount account, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        return await dbContext.Accounts.AsNoTracking().Where(candidate => candidate.Id == account.Id).Select(candidate => candidate.Status).SingleAsync(cancellationToken);
    }

    private async Task<List<AuditEntry>> GetAuditEntriesAsync(TestAccount account, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        return await dbContext.AuditEntries.AsNoTracking().Where(entry => entry.AccountId == account.Id).OrderBy(entry => entry.Id).ToListAsync(cancellationToken);
    }
    #endregion
}
