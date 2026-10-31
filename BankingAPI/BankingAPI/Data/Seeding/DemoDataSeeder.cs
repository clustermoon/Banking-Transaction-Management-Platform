using BankingAPI.Identity;

using Microsoft.AspNetCore.Identity;

namespace BankingAPI.Data.Seeding;

/// <summary>
/// Creates the demo customers through <see cref="CustomerRegistrar"/>, the same path as the
/// register endpoint. Idempotent: users that already exist are skipped, so restarts are safe.
/// </summary>
public sealed partial class DemoDataSeeder(
    UserManager<ApplicationUser> userManager,
    CustomerRegistrar customerRegistrar,
    ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo customer {Email}")]
    private static partial void LogSeededCustomer(ILogger logger, string email);
}
