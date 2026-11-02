using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Data.Seeding;

/// <summary>
/// Creates the demo customers, accounts, and history through the same code paths as real
/// activity: CustomerRegistrar for users, Account.Open and the lifecycle methods for accounts,
/// and LedgerService for every dollar. Safe to run on every startup: existing customers and
/// accounts are skipped, and recorded transactions are replayed rather than repeated.
/// </summary>
public sealed partial class DemoDataSeeder(
    UserManager<ApplicationUser> userManager,
    CustomerRegistrar customerRegistrar,
    BankingDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedCustomersAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var accounts = await OpenAccountsAsync(now, cancellationToken);
        await RecordHistoryAsync(accounts, now, cancellationToken);
        await ApplyStatusesAsync(cancellationToken);
        await VerifyLedgerAsync(accounts, cancellationToken);
    }

    private async Task SeedCustomersAsync(CancellationToken cancellationToken)
    {
        foreach (var demoUser in DemoCredentials.Customers)
        {
            if (await userManager.FindByEmailAsync(demoUser.Email) is not null)
            {
                continue;
            }

            var registration = await customerRegistrar.RegisterAsync(demoUser.Email, DemoCredentials.CustomerPassword, demoUser.DisplayName, cancellationToken);

            if (registration.User is null)
            {
                var errors = string.Join("; ", registration.Outcome.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Seeding demo customer {demoUser.Email} failed: {errors}");
            }

            LogSeededCustomer(logger, demoUser.Email);
        }
    }

    private async Task<Dictionary<string, (Guid Id, Guid OwnerId)>> OpenAccountsAsync(
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var demo in DemoBankingData.Accounts)
        {
            if (await dbContext.Accounts.AnyAsync(account => account.AccountNumber == demo.AccountNumber, cancellationToken))
            {
                continue;
            }

            var owner = await userManager.FindByEmailAsync(demo.OwnerEmail) ?? throw new InvalidOperationException($"Demo account owner {demo.OwnerEmail} doesn't exist.");
            dbContext.Accounts.Add(Account.Open(owner.Id, demo.Type, demo.Name, demo.AccountNumber, now.AddDays(-demo.OpenedDaysAgo)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var numbers = DemoBankingData.Accounts.Select(demo => demo.AccountNumber).ToList();
        return await dbContext.Accounts.AsNoTracking().Where(account => numbers.Contains(account.AccountNumber)).ToDictionaryAsync(account => account.AccountNumber, account => (account.Id, account.OwnerId), cancellationToken);
    }

    private async Task RecordHistoryAsync(Dictionary<string, (Guid Id, Guid OwnerId)> accounts, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // The real ledger service, given a clock set to each transaction's date. Oldest first,
        // so each account's ledger order matches its history's date order.
        var clock = new SeedingClock();
        var ledger = new LedgerService(dbContext, clock);

        foreach (var demo in DemoBankingData.Transactions.OrderByDescending(transaction => transaction.DaysAgo))
        {
            var (accountId, ownerId) = accounts[demo.AccountNumber];
            clock.SetUtcNow(now.AddDays(-demo.DaysAgo));
            var key = IdempotencyKeyFor(demo);

            switch (demo.Type)
            {
                case LedgerEntryType.Deposit:
                    await ledger.DepositAsync(new LedgerRequest(ownerId, accountId, demo.Amount, demo.Description, key), cancellationToken);
                    break;

                case LedgerEntryType.Withdrawal:
                    await ledger.WithdrawAsync(new LedgerRequest(ownerId, accountId, demo.Amount, demo.Description, key), cancellationToken);
                    break;

                case LedgerEntryType.TransferOut:
                    var destination = demo.ToAccountNumber ?? throw new InvalidOperationException($"Demo transfer from {demo.AccountNumber} has no destination.");
                    await ledger.TransferAsync(new TransferRequest(ownerId, accountId, destination, demo.Amount, demo.Description, key), cancellationToken);
                    break;

                default:
                    // An incoming transfer is recorded by its outgoing half, never written on its own.
                    throw new InvalidOperationException($"Demo transactions can't be of type {demo.Type}.");
            }
        }
    }

    private async Task ApplyStatusesAsync(CancellationToken cancellationToken)
    {
        // Balances were changed by SQL the change tracker never saw; load accounts fresh.
        dbContext.ChangeTracker.Clear();

        foreach (var demo in DemoBankingData.Accounts.Where(demo => demo.Status != AccountStatus.Active))
        {
            var account = await dbContext.Accounts.SingleAsync(candidate => candidate.AccountNumber == demo.AccountNumber, cancellationToken);
            if (account.Status == demo.Status) { continue; }

            if (demo.Status == AccountStatus.Frozen)
            {
                account.Freeze();
            }
            else
            {
                account.Close();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Refuse to start with demo data the ledger can't account for.
    private async Task VerifyLedgerAsync(
        Dictionary<string, (Guid Id, Guid OwnerId)> accounts, CancellationToken cancellationToken)
    {
        var violations = await LedgerInvariants.FindViolationsAsync(dbContext, [.. accounts.Values.Select(account => account.Id)], cancellationToken);

        if (violations.Count > 0)
        {
            throw new InvalidOperationException($"Seeded demo data violates ledger invariants: {string.Join(" ", violations)}");
        }
    }

    // Stable for each transaction, so a restart replays seeded history instead of repeating it.
    private static Guid IdempotencyKeyFor(DemoTransaction demo)
    {
        var identity = string.Create(CultureInfo.InvariantCulture, $"demo|{demo.AccountNumber}|{demo.DaysAgo}|{demo.Description}");
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(identity)).AsSpan(0, 16));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo customer {Email}")]
    private static partial void LogSeededCustomer(ILogger logger, string email);
}
