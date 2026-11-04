namespace BankingAPI.Data.Seeding;

internal static class DemoSeedingExtensions
{
    public static async Task SeedDemoDataIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Seeding:Enabled"))
        {
            return;
        }

        if (app.Environment.IsProduction())
        {
            throw new InvalidOperationException("Seeding:Enabled must not be enabled in Production.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var seeder = ActivatorUtilities.CreateInstance<DemoDataSeeder>(scope.ServiceProvider);
        await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
    }
}
