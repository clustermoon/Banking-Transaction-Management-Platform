using System.Security.Claims;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BankingClient.Authentication;

/// <summary>
/// Turns an API login into the client's cookie session. The API token is stored inside the
/// encrypted, HttpOnly cookie, so the browser never sees it, and the session expires exactly
/// when the token does.
/// </summary>
public static class ClientSession
{
    public const string AccessTokenName = "access_token";

    public static ClaimsPrincipal CreatePrincipal(ProfileResponse profile)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
            new(ClaimTypes.Name, profile.DisplayName),
            new(ClaimTypes.Email, profile.Email),
            .. profile.Roles.Select(role => new Claim(ClaimTypes.Role, role)),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static AuthenticationProperties CreateProperties(LoginResponse login)
    {
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = login.ExpiresAt,
            IsPersistent = false,
            AllowRefresh = false,
        };
        properties.StoreTokens([new AuthenticationToken { Name = AccessTokenName, Value = login.AccessToken }]);
        return properties;
    }
}
