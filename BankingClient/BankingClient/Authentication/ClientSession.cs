using System.Security.Claims;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BankingClient.Authentication;

/// <summary>
/// A static class that provides methods for creating a <see cref="ClaimsPrincipal"/> and <see cref="AuthenticationProperties"/> for a signed-in user based on their profile and login response. This class is used to manage the user's authentication session in the banking client application.
/// </summary>
public static class ClientSession
{
    public const string AccessTokenName = "access_token";

    /// <summary>
    /// Creates a <see cref="ClaimsPrincipal"/> for the signed-in user based on their profile information. The principal includes claims for the user's ID, display name, email, and roles, which are used for authorization and identity management within the application.
    /// </summary>
    /// <param name="profile"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Creates <see cref="AuthenticationProperties"/> for the signed-in user based on their login response. The properties include the expiration time of the authentication session and store the access token, which is used for API requests on behalf of the user.
    /// </summary>
    /// <param name="login"></param>
    /// <returns></returns>
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
