using System.Net;
using System.Text;

namespace BankingClient.Tests.Infrastructure;

/// <summary>
/// This class serves as a stub implementation of HttpMessageHandler, allowing for the simulation of HTTP responses in unit tests. It captures the last HTTP request made and returns a predefined HTTP response with a specified status code and optional JSON content. This is useful for testing how the banking client handles various API responses without making actual network calls.
/// </summary>
/// <param name="statusCode"></param>
/// <param name="json"></param>
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
