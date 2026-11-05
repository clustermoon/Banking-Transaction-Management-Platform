using BankingAPI.Data;
using BankingAPI.Domain.Accounts;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// This class provides test data for the database, including methods to create test accounts and users for testing purposes.
/// </summary>
internal static class DatabaseTestData
{
    /// <summary>
    /// Creates a test account asynchronously using the provided BankingApiFactory and CancellationToken. If an ownerId is not provided, a new user will be created for the account.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task<TestAccount> CreateAccountAsync(BankingApiFactory factory, CancellationToken cancellationToken) => CreateAccountAsync(factory, ownerId: null, cancellationToken);

    /// <summary>
    /// Creates a test account asynchronously using the provided BankingApiFactory, ownerId, and CancellationToken. If an ownerId is not provided, a new user will be created for the account.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="ownerId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Creates a new user asynchronously using the provided BankingApiFactory and CancellationToken, and returns the user's ID.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<Guid> CreateUserAsync(BankingApiFactory factory, CancellationToken cancellationToken)
    {
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.FindByEmailAsync(email))!.Id;
    }
}
