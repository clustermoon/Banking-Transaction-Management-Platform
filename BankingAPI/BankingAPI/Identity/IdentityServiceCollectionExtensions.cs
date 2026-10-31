using BankingAPI.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BankingAPI.Identity;

/// <summary>
/// Configures ASP.NET Core Identity, including user management, password policies,
/// account lockout, roles, and database persistence.
/// Keeps Identity configuration separate from application startup.
/// </summary>
internal static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddBankingIdentity(this IServiceCollection services)
    {
        // Identity
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            options.ClaimsIdentity.UserIdClaimType = JwtRegisteredClaimNames.Sub;
            options.ClaimsIdentity.EmailClaimType = JwtRegisteredClaimNames.Email;
            options.ClaimsIdentity.RoleClaimType = BankingClaimTypes.Role;
            options.ClaimsIdentity.SecurityStampClaimType = BankingClaimTypes.SecurityStamp;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<BankingDbContext>()
        .AddSignInManager();

        // Registrar
        services.AddScoped<CustomerRegistrar>();

        return services;
    }
}
