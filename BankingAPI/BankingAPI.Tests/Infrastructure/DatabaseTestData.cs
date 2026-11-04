using BankingAPI.Data;
using BankingAPI.Domain.Accounts;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// Creates database records for tests that need them. Accounts belong to a freshly registered
/// user unless an existing owner is given, so tests sharing one database never interfere.
/// </summary>
internal static class DatabaseTestData
{
    public static Task<TestAccount> CreateAccountAsync(BankingApiFactory factory, CancellationToken cancellationToken) => CreateAccountAsync(factory, ownerId: null, cancellationToken);

    public static async Task<TestAccount> CreateAccountAsync(BankingApiFactory factory, Guid? ownerId, CancellationToken cancellationToken)
    {
        var owner = ownerId ?? await CreateUserAsync(factory, cancellationToken);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var account = Account.Open(owner, AccountType.Checking, "Test Checking", AccountNumberGenerator.Generate(), DateTimeOffset.UtcNow);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TestAccount(account.Id, owner, account.AccountNumber);
    }

    public static async Task<Guid> CreateUserAsync(BankingApiFactory factory, CancellationToken cancellationToken)
    {
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.FindByEmailAsync(email))!.Id;
    }
}
