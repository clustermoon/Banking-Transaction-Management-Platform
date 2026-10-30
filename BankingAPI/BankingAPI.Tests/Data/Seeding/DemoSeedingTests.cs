using System.Net;

using BankingAPI.Data.Seeding;
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
    }
    #endregion
}
