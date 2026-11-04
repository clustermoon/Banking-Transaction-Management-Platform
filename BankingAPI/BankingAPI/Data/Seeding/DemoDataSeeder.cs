using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Data.Seeding;

public sealed partial class DemoDataSeeder(UserManager<ApplicationUser> userManager, CustomerRegistrar customerRegistrar, BankingDbContext dbContext, TimeProvider timeProvider, ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedCustomersAsync(cancellationToken);
        var adminId = await SeedAdminAsync();

        var now = timeProvider.GetUtcNow();
        var accounts = await OpenAccountsAsync(now, cancellationToken);
        await RecordHistoryAsync(accounts, adminId, now, cancellationToken);
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
                throw new InvalidOperationException(
                    $"Seeding demo customer {demoUser.Email} failed: {Describe(registration.Outcome)}");
            }

            LogSeededUser(logger, demoUser.Email);
        }
    }

    private async Task<Guid> SeedAdminAsync()
    {
        var admin = await userManager.FindByEmailAsync(DemoCredentials.AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = DemoCredentials.AdminEmail,
                Email = DemoCredentials.AdminEmail,
                DisplayName = DemoCredentials.AdminDisplayName,
            };
            EnsureSucceeded(await userManager.CreateAsync(admin, DemoCredentials.AdminPassword), "creating the demo admin");
            LogSeededUser(logger, DemoCredentials.AdminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin), "assigning the demo admin's role");
        }

        return admin.Id;
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

    private async Task RecordHistoryAsync(Dictionary<string, (Guid Id, Guid OwnerId)> accounts, Guid adminId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var clock = new SeedingClock();
        var ledger = new LedgerService(dbContext, clock);
        var administration = new AccountAdministrationService(dbContext, clock);

        var events = DemoBankingData.Transactions.Select(transaction => (transaction.DaysAgo, Event: (object)transaction)).Concat(DemoBankingData.StatusChanges.Select(change => (change.DaysAgo, Event: (object)change))).OrderByDescending(demoEvent => demoEvent.DaysAgo);

        foreach (var (daysAgo, demoEvent) in events)
        {
            clock.SetUtcNow(now.AddDays(-daysAgo));

            switch (demoEvent)
            {
                case DemoTransaction transaction:
                    await RecordTransactionAsync(ledger, transaction, accounts[transaction.AccountNumber], cancellationToken);
                    break;
                case DemoStatusChange change:
                    await RecordStatusChangeAsync(administration, change, accounts[change.AccountNumber].Id, adminId, cancellationToken);
                    break;
            }
        }
    }

    private static async Task RecordTransactionAsync(
        LedgerService ledger, DemoTransaction demo, (Guid Id, Guid OwnerId) account, CancellationToken cancellationToken)
    {
        var key = IdempotencyKeyFor(demo);

        switch (demo.Type)
        {
            case LedgerEntryType.Deposit:
                await ledger.DepositAsync(
                    new LedgerRequest(account.OwnerId, account.Id, demo.Amount, demo.Description, key), cancellationToken);
                break;

            case LedgerEntryType.Withdrawal:
                await ledger.WithdrawAsync(
                    new LedgerRequest(account.OwnerId, account.Id, demo.Amount, demo.Description, key), cancellationToken);
                break;

            case LedgerEntryType.TransferOut:
                var destination = demo.ToAccountNumber
                    ?? throw new InvalidOperationException($"Demo transfer from {demo.AccountNumber} has no destination.");
                await ledger.TransferAsync(
                    new TransferRequest(account.OwnerId, account.Id, destination, demo.Amount, demo.Description, key),
                    cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Demo transactions can't be of type {demo.Type}.");
        }
    }

    private async Task RecordStatusChangeAsync(AccountAdministrationService administration, DemoStatusChange change, Guid accountId, Guid adminId, CancellationToken cancellationToken)
    {
        if (await dbContext.AuditEntries.AnyAsync(
                entry => entry.AccountId == accountId && entry.ToStatus == change.ToStatus && entry.Reason == change.Reason,
                cancellationToken))
        {
            return;
        }

        dbContext.ChangeTracker.Clear();

        _ = change.ToStatus switch
        {
            AccountStatus.Frozen => await administration.FreezeAsync(adminId, accountId, change.Reason, cancellationToken),
            AccountStatus.Active => await administration.UnfreezeAsync(adminId, accountId, change.Reason, cancellationToken),
            AccountStatus.Closed => await administration.CloseAsync(adminId, accountId, change.Reason, cancellationToken),
            _ => throw new InvalidOperationException($"Demo status changes can't target {change.ToStatus}."),
        };
    }

    private async Task VerifyLedgerAsync(
        Dictionary<string, (Guid Id, Guid OwnerId)> accounts, CancellationToken cancellationToken)
    {
        var violations = await LedgerInvariants.FindViolationsAsync(dbContext, [.. accounts.Values.Select(account => account.Id)], cancellationToken);

        if (violations.Count > 0)
        {
            throw new InvalidOperationException($"Seeded demo data violates ledger invariants: {string.Join(" ", violations)}");
        }
    }

    private static Guid IdempotencyKeyFor(DemoTransaction demo)
    {
        var identity = string.Create(CultureInfo.InvariantCulture, $"demo|{demo.AccountNumber}|{demo.DaysAgo}|{demo.Description}");
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(identity)).AsSpan(0, 16));
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Seeding failed while {action}: {Describe(result)}");
        }
    }

    private static string Describe(IdentityResult result) => string.Join("; ", result.Errors.Select(error => error.Description));

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo user {Email}")]
    private static partial void LogSeededUser(ILogger logger, string email);
}
