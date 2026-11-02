using System.Net;
using System.Text;

namespace BankingClient.Tests.Infrastructure;

/// <summary>
/// Stands in for the network: every request gets the configured status and optional JSON body,
/// and the last request is kept so tests can check what the client sent.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string? json = null) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        if (json is not null)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return Task.FromResult(response);
    }
}
