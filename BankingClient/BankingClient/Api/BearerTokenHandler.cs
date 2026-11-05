using System.Net.Http.Headers;

using BankingClient.Authentication;

using Microsoft.AspNetCore.Authentication;

namespace BankingClient.Api;

/// <summary>
/// A delegating handler that adds a bearer token to outgoing HTTP requests if the request does not already have an authorization header. The token is retrieved from the current HTTP context's authentication session, allowing the banking API client to authenticate requests on behalf of the signed-in user.
/// </summary>
/// <param name="httpContextAccessor"></param>
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
