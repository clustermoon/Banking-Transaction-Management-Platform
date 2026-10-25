using BankingAPI.Identity;
using BankingAPI.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Data;

/// <summary>
/// The application's unit of work: Identity tables plus, as slices land, the banking schema.
/// </summary>
public sealed class BankingDbContext(DbContextOptions<BankingDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
    }
}

