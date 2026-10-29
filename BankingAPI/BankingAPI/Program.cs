using BankingAPI.Data;
using BankingAPI.Data.Seeding;
using BankingAPI.Identity;
using BankingAPI.Infrastructure.Errors;
using BankingAPI.Infrastructure.OpenApi;
using BankingAPI.Infrastructure.Validation;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

namespace BankingAPI;

public sealed class Program
{
    private Program()
    {
    }

    public static async Task Main(string[] args)
    {

        // --- DI Ioc Container Configurations ---
        var builder = WebApplication.CreateBuilder(args);

        // Persistence Services
        builder.Services.AddDbContext<BankingDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Banking")
                ?? throw new InvalidOperationException("Connection string 'Banking' is not configured.");
            options.UseSqlServer(connectionString);
        });

        // Identity and Authentication Services
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddBankingIdentity();
        builder.Services.AddBankingAuthentication();

        // HTTP Surface
        builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
        builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        // Readiness
        builder.Services.AddHealthChecks().AddDbContextCheck<BankingDbContext>(
        customTestQuery: static async (dbContext, cancellationToken) =>
        {
            // CanConnectAsync retries connection failures for up to a minute, which makes a
            // stopped database look like a hung endpoint. A plain query fails fast instead.
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);
            return true;
        });

        // --- Middleware Configurations ---
        var app = builder.Build();

        // Migrate database into the program
        await app.MigrateDatabaseIfEnabledAsync();
        await app.SeedDemoDataIfEnabledAsync();

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        // Docker Health Probe
        app.MapHealthChecks("/health/ready").AllowAnonymous();
        app.MapControllers();

        // Display UI for the API while in development
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Demo"))
        {
            var apiReference = app.MapGroup(string.Empty).AllowAnonymous();
            apiReference.MapOpenApi();
            apiReference.MapScalarApiReference();
        }

        await app.RunAsync();
    }
}
