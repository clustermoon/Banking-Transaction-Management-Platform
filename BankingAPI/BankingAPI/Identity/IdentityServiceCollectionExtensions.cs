using BankingAPI.Data;
using BankingAPI.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BankingAPI.Identity;

internal static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddBankingIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Length over composition, following NIST SP 800-63B: composition rules push
                // users toward predictable patterns without adding real strength.
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Read the same claim names the access token carries.
                options.ClaimsIdentity.UserIdClaimType = JwtRegisteredClaimNames.Sub;
                options.ClaimsIdentity.EmailClaimType = JwtRegisteredClaimNames.Email;
                options.ClaimsIdentity.RoleClaimType = BankingClaimTypes.Role;
                options.ClaimsIdentity.SecurityStampClaimType = BankingClaimTypes.SecurityStamp;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<BankingDbContext>()
            .AddSignInManager();

        return services;
    }
}
