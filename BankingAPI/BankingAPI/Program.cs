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

/// <summary>
/// Defines the entry point and composition root for the Banking API.
/// </summary>
/// <remarks>
/// This class brings together the application's infrastructure, identity,
/// and HTTP services through dependency injection. It also establishes
/// the request-processing pipeline and coordinates startup operations.
///
/// Keeping these responsibilities centralized provides a clear picture
/// of how the application is assembled without requiring individual
/// features to manage their own hosting infrastructure or startup behavior.
/// </remarks>
public sealed class Program
{
    private Program()
    {
    }

    /// <summary>
    /// Configures and starts the ASP.NET Core application host.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments supplied to the application.
    /// </param>
    /// <returns>
    /// A task representing the application's execution until shutdown.
    /// </returns>
    public static async Task Main(string[] args)
    {

        // --- DI Ioc Container Configurations ---
        //
        // Create the application builder, which provides access to the host's configuration.
        // All service registrations are completed before building the host. 
        var builder = WebApplication.CreateBuilder(args);

        // --- Persistence Infrastructure ---
        //
        // Register the EF Core database context with the SQL Server as its persistence provider.
        // Our connection string is resolved from the application configuration.
        builder.Services.AddDbContext<BankingDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Banking")
                ?? throw new InvalidOperationException("Connection string 'Banking' is not configured.");
            options.UseSqlServer(connectionString);
        });

        // --- Identity and Authentication ---
        //
        // Register a shared clock abstraction so we can inject it compontents that need it.
        // Identity and authentication configuration are delegated to dedicated extensions to keep program.cs modular.
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddBankingIdentity();
        builder.Services.AddBankingAuthentication();

        // --- HTTP and Application Services ---
        //
        // Register controller-based HTTP endpoints and attach the shared 'FluentVaidation' filter to the controller
        // execution pipeline. Validators are discovered from the API assembly including internal types without needing registration. 
        builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());
        builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

        // Centralize exception handling and error-response infrastructure rather than requiring individual controllers to handle
        // unexpected applicaiton failures. OpenAPI generation is also registered here. End points are mapped later in the startup process. 
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        // --- Database Readiness ---
        //
        // Register a readiness check that verifies the application and if it can execute a minimal SQL command against the databse.
        // A direct SQL command is used instead of the default connection probe to avoid its potentially lengthly failure.
        builder.Services.AddHealthChecks().AddDbContextCheck<BankingDbContext>(
        customTestQuery: static async (dbContext, cancellationToken) =>
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);
            return true;
        });

        // --- Application Initialization ---
        //
        // Build the host after completing service registration.
        var app = builder.Build();

        // Apply database migrations and insert demonstration data only when their respective startup options are enabled.
        // Migration precedes seeding so that demonstration records are created against the expected schema. 
        await app.MigrateDatabaseIfEnabledAsync();
        await app.SeedDemoDataIfEnabledAsync();

        // --- HTTP Request Pipeline ---
        //
        // Establis centralized exception handling before downstream middleware and endpoints execute.
        // Authentication must run before authorization. Authentication establishes the request identity which authorization needs.
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/ready").AllowAnonymous();
        app.MapControllers();

        // --- Demonstration Documentation ---
        // Publish the OpenAPI and Scalar's interactive API refernce for development and demonstration (the default for this-
        // portfolio project). We allow anonymous access to these documentation endpoitns to make the endpoints easier to inspect.
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Demo"))
        {
            var apiReference = app.MapGroup(string.Empty).AllowAnonymous();
            apiReference.MapOpenApi();
            apiReference.MapScalarApiReference();
        }

        // Vroom. Starts the back-end program.
        await app.RunAsync();
    }
}
