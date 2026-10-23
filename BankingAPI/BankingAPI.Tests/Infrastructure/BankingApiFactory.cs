using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;
namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// Hosts the API in memory against a disposable SQL Server container, so tests exercise
/// the real database engine (constraints, triggers, locking) rather than a fake.
/// One instance is shared by the whole test assembly.
/// </summary>
public sealed class BankingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async ValueTask InitializeAsync() => await _sqlServer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The container's default connection string targets master; point it at an app database.
        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "Banking",
        }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Banking", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }
}
