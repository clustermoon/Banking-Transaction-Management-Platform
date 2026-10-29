using System.Net;

using BankingAPI.Data.Seeding;
using BankingAPI.Identity;
using BankingAPI.Infrastructure;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Data;

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

    [Theory]
    [MemberData(nameof(DemoCustomerEmails))]
    public async Task DemoCustomerCanSignInWithPublishedPassword(string email)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.LoginAsync(email, DemoCredentials.CustomerPassword, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RestartingTheApiDoesNotDuplicateDemoCustomers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // A second host runs startup again, including the seeder, against the same database.
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
}
