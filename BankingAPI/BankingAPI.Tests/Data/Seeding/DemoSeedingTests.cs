using System.Net;
using System.Net.Http.Json;

using BankingAPI.Data;
using BankingAPI.Data.Seeding;
using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;
using BankingAPI.Features.Profile;
using BankingAPI.Identity;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Data.Seeding;

public sealed class DemoSeedingTests(BankingApiFactory factory)
{
    public static TheoryData<string> DemoCustomerEmails()
    {
        var emails = new TheoryData<string>();
        foreach (var customer in DemoCredentials.Customers)
        {
            emails.Add(customer.Email);
        }

        return emails;
    }

    #region THEORY? - Demo Customer Can Sign In WIth Published Password
    [Theory]
    [MemberData(nameof(DemoCustomerEmails))]
    public async Task DemoCustomerCanSignInWithPublishedPassword(string email)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.LoginAsync(email, DemoCredentials.CustomerPassword, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    #endregion

    #region FACT? - Restarting The API Does Not Duplicate Demo Customers
    [Fact]
    public async Task RestartingTheApiDoesNotDuplicateDemoCustomers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var restartedHost = factory.WithWebHostBuilder(static _ => { });
        using var restartedClient = restartedHost.CreateClient();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var customer in DemoCredentials.Customers)
        {
            var matches = await userManager.Users.CountAsync(user => user.Email == customer.Email, cancellationToken);
            Assert.Equal(1, matches);
        }
        // Accounts and history must also exist exactly once after the restart replays seeding.
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var demoNumbers = DemoBankingData.Accounts.Select(demo => demo.AccountNumber).ToList();
        var demoAccountIds = await dbContext.Accounts.AsNoTracking().Where(account => demoNumbers.Contains(account.AccountNumber)).Select(account => account.Id).ToListAsync(cancellationToken);
        var entryCount = await dbContext.LedgerEntries.CountAsync(entry => demoAccountIds.Contains(entry.AccountId), cancellationToken);

        Assert.Equal(DemoBankingData.Accounts.Count, demoAccountIds.Count);
        var expectedEntries = DemoBankingData.Transactions.Sum(transaction => transaction.Type == LedgerEntryType.TransferOut ? 2 : 1);
        Assert.Equal(expectedEntries, entryCount);

        var auditCount = await dbContext.AuditEntries.CountAsync(entry => demoAccountIds.Contains(entry.AccountId), cancellationToken);
        Assert.Equal(DemoBankingData.StatusChanges.Count, auditCount);
    }
    #endregion

    #region FACT? - Demo Accounts Match The Published Data
    [Fact]
    public async Task DemoAccountsMatchThePublishedData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var numbers = DemoBankingData.Accounts.Select(demo => demo.AccountNumber).ToList();

        var accounts = await dbContext.Accounts.AsNoTracking().Where(account => numbers.Contains(account.AccountNumber)).ToDictionaryAsync(account => account.AccountNumber, cancellationToken);

        Assert.Equal(DemoBankingData.Accounts.Count, accounts.Count);
        foreach (var demo in DemoBankingData.Accounts)
        {
            var expected = DemoBankingData.StatusChanges.Where(change => change.AccountNumber == demo.AccountNumber).OrderBy(change => change.DaysAgo).Select(change => (AccountStatus?)change.ToStatus).FirstOrDefault() ?? AccountStatus.Active;
            Assert.Equal(expected, accounts[demo.AccountNumber].Status);
        }

        Assert.Equal(23.40m, accounts[DemoBankingData.CaraCheckingNumber].Balance);
        Assert.Equal(0m, accounts[DemoBankingData.CaraClosedCheckingNumber].Balance);
        Assert.Equal(3601.05m, accounts[DemoBankingData.AliceCheckingNumber].Balance);
        Assert.Equal(6500.00m, accounts[DemoBankingData.AliceSavingsNumber].Balance);
        Assert.Equal(4237.55m, accounts[DemoBankingData.BenCheckingNumber].Balance);
    }
    #endregion

    #region FACT? - Demo Ledger Is Consistent
    [Fact]
    public async Task DemoLedgerIsConsistent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var numbers = DemoBankingData.Accounts.Select(demo => demo.AccountNumber).ToList();
        var accountIds = await dbContext.Accounts.AsNoTracking()
            .Where(account => numbers.Contains(account.AccountNumber))
            .Select(account => account.Id)
            .ToListAsync(cancellationToken);

        Assert.Empty(await LedgerInvariants.FindViolationsAsync(dbContext, accountIds, cancellationToken));
    }
    #endregion

    #region FACT? - Demo Audit Trail Records Every Status Change By The Admin
    [Fact]
    public async Task DemoAuditTrailRecordsEveryStatusChangeByTheAdmin()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(DemoCredentials.AdminEmail);
        var accountIds = await GetDemoAccountIdsAsync(dbContext, cancellationToken);

        var entries = await dbContext.AuditEntries.AsNoTracking()
            .Where(entry => accountIds.Values.Contains(entry.AccountId))
            .ToListAsync(cancellationToken);

        Assert.Equal(DemoBankingData.StatusChanges.Count, entries.Count);
        Assert.All(entries, entry => Assert.Equal(admin!.Id, entry.ActorUserId));
        foreach (var change in DemoBankingData.StatusChanges)
        {
            Assert.Contains(entries, entry =>
                entry.AccountId == accountIds[change.AccountNumber]
                && entry.ToStatus == change.ToStatus
                && entry.Reason == change.Reason);
        }
    }
    #endregion

    #region FACT? - Demo Admin Signs In As An Admin Only
    [Fact]
    public async Task DemoAdminSignsInAsAnAdminOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        await client.SignInAsync(DemoCredentials.AdminEmail, DemoCredentials.AdminPassword, cancellationToken);

        var profile = await client.GetFromJsonAsync<ProfileResponse>(
            "/api/profile", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        // Provisioned with only the Admin role: never also a customer.
        Assert.Equal([Roles.Admin], profile!.Roles);
    }
    #endregion


    private static Task<Dictionary<string, Guid>> GetDemoAccountIdsAsync(
        BankingDbContext dbContext, CancellationToken cancellationToken)
    {
        var numbers = DemoBankingData.Accounts.Select(demo => demo.AccountNumber).ToList();
        return dbContext.Accounts.AsNoTracking()
            .Where(account => numbers.Contains(account.AccountNumber))
            .ToDictionaryAsync(account => account.AccountNumber, account => account.Id, cancellationToken);
    }

}
