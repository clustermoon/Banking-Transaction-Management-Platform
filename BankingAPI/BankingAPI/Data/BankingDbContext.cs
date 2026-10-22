using Microsoft.EntityFrameworkCore;
namespace BankingAPI.Data;

/// <summary>
/// The application's Banking data base context.
/// </summary>
public sealed class BankingDbContext(DbContextOptions<BankingDbContext> options) : DbContext(options);

