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
        services.AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

        services.AddSingleton<AccessTokenIssuer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured from validated JwtOptions at resolve time, so test and container
        // configuration overrides apply.
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

                // One issuer and validator in the same process: no clock drift to absorb,
                // so a 30-minute token means exactly 30 minutes.
                ClockSkew = TimeSpan.Zero,
            };
            bearer.Events = new JwtBearerEvents { OnTokenValidated = TokenRevocationCheck.ValidateAsync };
        });

        // Deny by default: an endpoint someone forgets to protect is still protected.
        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
