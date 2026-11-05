using BankingAPI.Data;

using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Identity;

public sealed class CustomerRegistrar(UserManager<ApplicationUser> userManager, BankingDbContext dbContext)
{
    public async Task<CustomerRegistrationResult> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName.Trim(),
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return new CustomerRegistrationResult(created, User: null);
        }

        var roleAssigned = await userManager.AddToRoleAsync(user, Roles.Customer);
        if (!roleAssigned.Succeeded)
        {
            throw new InvalidOperationException($"Assigning the {Roles.Customer} role to a new user failed.");
        }

        await transaction.CommitAsync(cancellationToken);

        return new CustomerRegistrationResult(IdentityResult.Success, user);
    }
}
