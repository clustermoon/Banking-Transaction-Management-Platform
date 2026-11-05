using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;
namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// This class provides a factory for creating a test instance of the Banking API application, including configuration for the test database and other settings.
/// </summary>
public sealed class BankingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// SQL Server container used for testing purposes, built using the Testcontainers library and configured with the latest SQL Server 2022 image.
    /// </summary>
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    /// <summary>
    /// Initializes the test instance of the Banking API application, including starting the SQL Server container and configuring the application settings for testing purposes.
    /// </summary>
    /// <returns></returns>
    public async ValueTask InitializeAsync()
    {
        await _sqlServer.StartAsync();

        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString()) { InitialCatalog = "Banking", }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Banking", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-0123456789abcdef");
        builder.UseSetting("Seeding:Enabled", "true");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }
}
