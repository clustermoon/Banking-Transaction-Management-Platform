using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BankingAPI.Identity;

internal static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddBankingAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();

        services.AddSingleton<AccessTokenIssuer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;

            bearer.MapInboundClaims = false;
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = jwt.CreateSigningKey(),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = BankingClaimTypes.Role,

                ClockSkew = TimeSpan.Zero,
            };
            bearer.Events = new JwtBearerEvents { OnTokenValidated = TokenRevocationCheck.ValidateAsync };
        });

        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()).AddPolicy(Policies.Customer, policy => policy.RequireRole(Roles.Customer));

        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()).AddPolicy(Policies.Customer, policy => policy.RequireRole(Roles.Customer)).AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin));

        return services;
    }
}
