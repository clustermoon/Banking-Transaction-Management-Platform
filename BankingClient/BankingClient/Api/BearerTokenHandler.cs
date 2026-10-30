using System.Net.Http.Headers;

using BankingClient.Authentication;

using Microsoft.AspNetCore.Authentication;

namespace BankingClient.Api;

/// <summary>
/// Attaches the signed-in user's API token, read from their encrypted session cookie, to
/// outgoing API calls. Requests that already carry a token (sign-in) are left alone.
/// </summary>
public sealed class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && httpContextAccessor.HttpContext is { } httpContext)
        {
            var accessToken = await httpContext.GetTokenAsync(ClientSession.AccessTokenName);
            if (accessToken is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
