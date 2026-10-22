using Microsoft.EntityFrameworkCore;
namespace BankingAPI.Data;

internal static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations when <c>Database:MigrateOnStartup</c> is set, so a fresh
    /// container or clone is usable without running EF tooling. Production deployment
    /// apply migrations as a separate, reviewed step, so the flag is refused there.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static async Task MigrateDatabaseIfEnabledAsync(this WebApplication app)
    { 
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup")) { return; }

        // Restrict the migrate on startup
        if (app.Environment.IsProduction())
        {
            throw new InvalidOperationException("Database:MigrateOnStartup must not be enabled in production");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
