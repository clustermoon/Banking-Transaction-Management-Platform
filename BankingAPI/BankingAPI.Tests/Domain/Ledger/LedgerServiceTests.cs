using BankingAPI.Data;
using BankingAPI.Domain;
using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;
using BankingAPI.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Domain.Ledger;

public sealed class LedgerServiceTests(BankingApiFactory factory)
{
    #region FACT? - Deposit Raises The Balance And Records The Entry
    [Fact]
    public async Task DepositRaisesTheBalanceAndRecordsTheEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);

        var result = await RunAsync(ledger => ledger.DepositAsync(Request(account, 25.50m), cancellationToken));

        Assert.False(result.Replayed);
        Assert.Equal(25.50m, result.Entry.BalanceAfter);
        Assert.Equal(25.50m, await GetBalanceAsync(account, cancellationToken));
        await AssertLedgerConsistentAsync(account, cancellationToken);
    }
    #endregion

    #region FACT? - Withdrawal Beyond The Balance Is Refused And Changes Nothing
    [Fact]
    public async Task WithdrawalBeyondTheBalanceIsRefusedAndChangesNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(account, 10m), cancellationToken));

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => RunAsync(ledger => ledger.WithdrawAsync(Request(account, 10.01m), cancellationToken)));

        Assert.Equal("Insufficient funds for this withdrawal.", error.Message);
        Assert.Equal(10m, await GetBalanceAsync(account, cancellationToken));
        await AssertLedgerConsistentAsync(account, cancellationToken);
    }
    #endregion

    #region FACT? - Parallel Withdrawals Never Overdraw
    [Fact]
    public async Task ParallelWithdrawalsNeverOverdraw()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(account, 100m), cancellationToken));

        // Twenty $10 withdrawals against $100, all at once: exactly ten can succeed.
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => TryWithdrawAsync(account, 10m, cancellationToken)));

        Assert.Equal(10, outcomes.Count(succeeded => succeeded));
        Assert.Equal(0m, await GetBalanceAsync(account, cancellationToken));
        await AssertLedgerConsistentAsync(account, cancellationToken);
    }
    #endregion

    #region FACT? - Frozen Account Cannot Move Money
    [Fact]
    public async Task FrozenAccountCannotMoveMoney()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
            var stored = await dbContext.Accounts.SingleAsync(candidate => candidate.Id == account.Id, cancellationToken);
            stored.Freeze();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => RunAsync(ledger => ledger.DepositAsync(Request(account, 10m), cancellationToken)));

        Assert.Contains("frozen", error.Message, StringComparison.Ordinal);
        Assert.Equal(0m, await GetBalanceAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Another User's Account Is Not Found
    [Fact]
    public async Task AnotherUsersAccountIsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var intruder = new LedgerRequest(Guid.NewGuid(), account.Id, 10m, "Test", Guid.NewGuid());

        await Assert.ThrowsAsync<AccountNotFoundException>(() => RunAsync(ledger => ledger.DepositAsync(intruder, cancellationToken)));

        Assert.Equal(0m, await GetBalanceAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Replayed Request Moves Money Once
    [Fact]
    public async Task ReplayedRequestMovesMoneyOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var request = Request(account, 40m);

        var first = await RunAsync(ledger => ledger.DepositAsync(request, cancellationToken));
        var retry = await RunAsync(ledger => ledger.DepositAsync(request, cancellationToken));

        Assert.True(retry.Replayed);
        Assert.Equal(first.Entry.Id, retry.Entry.Id);
        Assert.Equal(40m, await GetBalanceAsync(account, cancellationToken));
        await AssertLedgerConsistentAsync(account, cancellationToken);
    }
    #endregion

    #region FACT? - Reused Key With A Different Request Is Refused
    [Fact]
    public async Task ReusedKeyWithADifferentRequestIsRefused()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var key = Guid.NewGuid();
        await RunAsync(ledger => ledger.DepositAsync(Request(account, 10m, key), cancellationToken));

        await Assert.ThrowsAsync<BusinessRuleException>(() => RunAsync(ledger => ledger.DepositAsync(Request(account, 20m, key), cancellationToken)));

        Assert.Equal(10m, await GetBalanceAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Concurrent Duplicates Move Money Once
    [Fact]
    public async Task ConcurrentDuplicatesMoveMoneyOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var request = Request(account, 50m);

        // Ten identical requests at once, as a double-clicked button or a client retry storm would send.
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => RunAsync(ledger => ledger.DepositAsync(request, cancellationToken))));

        Assert.Single(results.Select(result => result.Entry.Id).Distinct());
        Assert.Single(results, result => !result.Replayed);
        Assert.Equal(50m, await GetBalanceAsync(account, cancellationToken));
        await AssertLedgerConsistentAsync(account, cancellationToken);
    }
    #endregion

    #region Helpers
    private static LedgerRequest Request(TestAccount account, decimal amount, Guid? key = null) => new(account.OwnerId, account.Id, amount, "Test", key ?? Guid.NewGuid());

    // Each operation gets its own scope, and so its own DbContext and connection,
    // exactly as separate HTTP requests would.
    private async Task<LedgerOperationResult> RunAsync(Func<LedgerService, Task<LedgerOperationResult>> operation)
    {
        using var scope = factory.Services.CreateScope();
        return await operation(scope.ServiceProvider.GetRequiredService<LedgerService>());
    }

    private async Task<bool> TryWithdrawAsync(TestAccount account, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(ledger => ledger.WithdrawAsync(Request(account, amount), cancellationToken));
            return true;
        }
        catch (BusinessRuleException)
        {
            return false;
        }
    }

    private async Task<decimal> GetBalanceAsync(TestAccount account, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        return await dbContext.Accounts.AsNoTracking()
            .Where(candidate => candidate.Id == account.Id)
            .Select(candidate => candidate.Balance)
            .SingleAsync(cancellationToken);
    }

    private async Task AssertLedgerConsistentAsync(TestAccount account, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        Assert.Empty(await LedgerInvariants.FindViolationsAsync(dbContext, [account.Id], cancellationToken));
    }
    #endregion
}
