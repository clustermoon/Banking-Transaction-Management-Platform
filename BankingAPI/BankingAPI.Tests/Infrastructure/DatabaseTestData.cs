using BankingAPI.Data;
using BankingAPI.Domain.Accounts;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// Creates database records for tests that need them, each owned by a freshly registered
/// user, so tests sharing one database never interfere.
/// </summary>
internal static class DatabaseTestData
{
    public static async Task<Guid> CreateAccountAsync(BankingApiFactory factory, CancellationToken cancellationToken)
    {
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var owner = await userManager.FindByEmailAsync(email);

        var account = Account.Open(owner!.Id, AccountType.Checking, "Test Checking", AccountNumberGenerator.Generate(), DateTimeOffset.UtcNow);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
