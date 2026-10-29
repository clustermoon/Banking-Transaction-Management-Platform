using BankingAPI.Data;
using BankingAPI.Identity;
using BankingAPI.Infrastructure.Errors;
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

        // DI Ioc Container Configurations
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<BankingDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Banking")
                ?? throw new InvalidOperationException("Connection string 'Banking' is not configured.");
            options.UseSqlServer(connectionString);
        });
        builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
        builder.Services.AddOpenApi();

        builder.Services.AddHealthChecks().AddDbContextCheck<BankingDbContext>(
        customTestQuery: static async (dbContext, cancellationToken) =>
        {
            // CanConnectAsync retries connection failures for up to a minute, which makes a
            // stopped database look like a hung endpoint. A plain query fails fast instead.
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);
            return true;
        });
        builder.Services.AddBankingIdentity();


        // Middleware Configurations
        var app = builder.Build();
        await app.MigrateDatabaseIfEnabledAsync();

        // Display UI for the API while in development
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Demo"))
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapHealthChecks("/health/ready");
        app.MapControllers();

        await app.RunAsync();
    }
}
