using BankingAPI.Data;
using BankingAPI.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI;

public sealed class Program
{
    private Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<BankingDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Banking")
                ?? throw new InvalidOperationException("Connection string 'Banking' is not configured.");
            options.UseSqlServer(connectionString);
        });
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddHealthChecks().AddDbContextCheck<BankingDbContext>(
            customTestQuery: static async (dbContext, cancellationToken) =>
            {
                // CanConnectAsync retries connection failures for up to a minute, which makes a
                // stopped database look like a hung endpoint. A plain query fails fast instead.
                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);
                return true;
            });

        var app = builder.Build();

        await app.MigrateDatabaseIfEnabledAsync();

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapHealthChecks("/health/ready");
        app.MapControllers();

        await app.RunAsync();
    }
}
