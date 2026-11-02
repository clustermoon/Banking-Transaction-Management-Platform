using System.Net;

using BankingAPI.Data;
using BankingAPI.Data.Seeding;
using BankingAPI.Domain.Ledger;
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
        Assert.Equal(DemoBankingData.Transactions.Count, entryCount);
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

        var accounts = await dbContext.Accounts.AsNoTracking()
            .Where(account => numbers.Contains(account.AccountNumber))
            .ToDictionaryAsync(account => account.AccountNumber, cancellationToken);

        Assert.Equal(DemoBankingData.Accounts.Count, accounts.Count);
        foreach (var demo in DemoBankingData.Accounts)
        {
            Assert.Equal(demo.Status, accounts[demo.AccountNumber].Status);
        }

        Assert.Equal(23.40m, accounts[DemoBankingData.CaraCheckingNumber].Balance);
        Assert.Equal(0m, accounts[DemoBankingData.CaraClosedCheckingNumber].Balance);
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
}
