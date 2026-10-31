using System.Net;
using System.Text;

namespace BankingClient.Tests.Infrastructure;

/// <summary>
/// Stands in for the network: every request gets the configured status and optional JSON body,
/// so API-client behavior can be tested without a running API.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string? json = null) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        if (json is not null)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/problem+json");
        }

        return Task.FromResult(response);
    }
}
