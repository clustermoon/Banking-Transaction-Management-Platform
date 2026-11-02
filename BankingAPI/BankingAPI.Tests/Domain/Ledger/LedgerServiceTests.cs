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

    #region FACT? - Transfer Moves Money As One Balanced Pair
    [Fact]
    public async Task TransferMovesMoneyAsOneBalancedPair()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sender = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var receiver = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(sender, 100m), cancellationToken));

        var result = await RunAsync(ledger => ledger.TransferAsync(Transfer(sender, receiver, 30m), cancellationToken));

        Assert.Equal(70m, await GetBalanceAsync(sender, cancellationToken));
        Assert.Equal(30m, await GetBalanceAsync(receiver, cancellationToken));

        // Another customer's account appears only as a masked number, never by name.
        Assert.Equal($"Transfer to •••• {receiver.AccountNumber[^4..]}", result.Entry.Description);
        await AssertLedgerConsistentAsync([sender, receiver], cancellationToken);
    }
    #endregion

    #region FACT? - Transfer Between Your Own Accounts Names Them
    [Fact]
    public async Task TransferBetweenYourOwnAccountsNamesThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var checking = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var savings = await DatabaseTestData.CreateAccountAsync(factory, checking.OwnerId, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(checking, 100m), cancellationToken));

        var result = await RunAsync(ledger => ledger.TransferAsync(
            Transfer(checking, savings, 25m) with { Note = "Monthly savings" }, cancellationToken));

        Assert.Equal("Transfer to Test Checking: Monthly savings", result.Entry.Description);
        await AssertLedgerConsistentAsync([checking, savings], cancellationToken);
    }
    #endregion

    #region FACT? - Failed Transfer Changes Neither Account
    [Fact]
    public async Task FailedTransferChangesNeitherAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sender = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var receiver = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(sender, 100m), cancellationToken));

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            RunAsync(ledger => ledger.TransferAsync(Transfer(sender, receiver, 100.01m), cancellationToken)));

        Assert.Equal("Insufficient funds for this transfer.", error.Message);
        Assert.Equal(100m, await GetBalanceAsync(sender, cancellationToken));
        Assert.Equal(0m, await GetBalanceAsync(receiver, cancellationToken));
        await AssertLedgerConsistentAsync([sender, receiver], cancellationToken);
    }
    #endregion

    #region FACT? - Frozen Destination Is Indistinguishable From An Unknown Number
    [Fact]
    public async Task FrozenDestinationIsIndistinguishableFromAnUnknownNumber()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sender = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var frozen = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(sender, 100m), cancellationToken));
        await FreezeAsync(frozen, cancellationToken);
        var unknown = frozen with { AccountNumber = "0000000000" };

        var toFrozen = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            RunAsync(ledger => ledger.TransferAsync(Transfer(sender, frozen, 10m), cancellationToken)));
        var toUnknown = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            RunAsync(ledger => ledger.TransferAsync(Transfer(sender, unknown, 10m), cancellationToken)));

        Assert.Equal(toUnknown.Message, toFrozen.Message);
        Assert.Equal(100m, await GetBalanceAsync(sender, cancellationToken));
    }
    #endregion

    #region FACT? - Transfer To The Same Account Is Refused
    [Fact]
    public async Task TransferToTheSameAccountIsRefused()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var account = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(account, 50m), cancellationToken));

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            RunAsync(ledger => ledger.TransferAsync(Transfer(account, account, 10m), cancellationToken)));

        Assert.Equal("Choose a different account to transfer to.", error.Message);
        Assert.Equal(50m, await GetBalanceAsync(account, cancellationToken));
    }
    #endregion

    #region FACT? - Transfer From Another Customer's Account Is Not Found
    [Fact]
    public async Task TransferFromAnotherCustomersAccountIsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var victim = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var thief = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(victim, 100m), cancellationToken));

        // The thief names the victim's account as the source and their own as the destination.
        var theft = Transfer(victim, thief, 100m) with { UserId = thief.OwnerId };

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            RunAsync(ledger => ledger.TransferAsync(theft, cancellationToken)));

        Assert.Equal(100m, await GetBalanceAsync(victim, cancellationToken));
    }
    #endregion

    #region FACT? - Opposing Transfers Neither Deadlock Nor Lose Money
    [Fact]
    public async Task OpposingTransfersNeitherDeadlockNorLoseMoney()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var alice = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var ben = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(alice, 100m), cancellationToken));
        await RunAsync(ledger => ledger.DepositAsync(Request(ben, 100m), cancellationToken));

        // Forty transfers at once, half in each direction. Without ordered locking, opposing
        // pairs deadlock; SQL Server would kill one with error 1205, which this test doesn't
        // catch, so any deadlock fails it.
        await Task.WhenAll(Enumerable.Range(0, 20).SelectMany(_ => new[]
        {
            TryTransferAsync(alice, ben, 10m, cancellationToken),
            TryTransferAsync(ben, alice, 10m, cancellationToken),
        }));

        var total = await GetBalanceAsync(alice, cancellationToken) + await GetBalanceAsync(ben, cancellationToken);
        Assert.Equal(200m, total);
        await AssertLedgerConsistentAsync([alice, ben], cancellationToken);
    }
    #endregion

    #region FACT? - Replayed Transfer Moves Money Once
    [Fact]
    public async Task ReplayedTransferMovesMoneyOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sender = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var receiver = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        await RunAsync(ledger => ledger.DepositAsync(Request(sender, 100m), cancellationToken));
        var request = Transfer(sender, receiver, 40m);

        var first = await RunAsync(ledger => ledger.TransferAsync(request, cancellationToken));
        var retry = await RunAsync(ledger => ledger.TransferAsync(request, cancellationToken));

        Assert.True(retry.Replayed);
        Assert.Equal(first.Entry.Id, retry.Entry.Id);
        Assert.Equal(60m, await GetBalanceAsync(sender, cancellationToken));
        Assert.Equal(40m, await GetBalanceAsync(receiver, cancellationToken));
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

    private Task AssertLedgerConsistentAsync(TestAccount account, CancellationToken cancellationToken) => AssertLedgerConsistentAsync([account], cancellationToken);

    private static TransferRequest Transfer(TestAccount from, TestAccount to, decimal amount, Guid? key = null) =>
    new(from.OwnerId, from.Id, to.AccountNumber, amount, Note: null, key ?? Guid.NewGuid());

    private async Task<bool> TryTransferAsync(
        TestAccount from, TestAccount to, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(ledger => ledger.TransferAsync(Transfer(from, to, amount), cancellationToken));
            return true;
        }
        catch (BusinessRuleException)
        {
            return false;
        }
    }

    private async Task FreezeAsync(TestAccount account, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var stored = await dbContext.Accounts.SingleAsync(candidate => candidate.Id == account.Id, cancellationToken);
        stored.Freeze();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AssertLedgerConsistentAsync(
        IReadOnlyCollection<TestAccount> accounts, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        Assert.Empty(await LedgerInvariants.FindViolationsAsync(
            dbContext, [.. accounts.Select(account => account.Id)], cancellationToken));
    }

    #endregion
}
