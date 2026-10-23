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

        var connectionString = builder.Configuration.GetConnectionString("Banking") ?? throw new InvalidOperationException("Connection string 'Banking' is not configured.");

        builder.Services.AddDbContext<BankingDbContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddHealthChecks().AddDbContextCheck<BankingDbContext>();
        builder.Services.AddDbContext<BankingDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Banking")
                ?? throw new InvalidOperationException("Connection string 'Banking' is not configured.");
            options.UseSqlServer(connectionString);
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
