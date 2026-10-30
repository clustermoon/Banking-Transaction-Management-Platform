using System.Net;

using BankingClient.Api;
using BankingClient.Api.Contracts;
using BankingClient.Tests.Infrastructure;

namespace BankingClient.Tests.Api;

public sealed class BankingApiClientTests
{
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string? json = null) =>
        new(new StubHttpMessageHandler(statusCode, json)) { BaseAddress = new Uri("http://api.test/") };

    [Fact]
    public async Task GetProfileThrowsUnauthorizedWhenTheApiRejectsTheToken()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.Unauthorized);
        var api = new BankingApiClient(httpClient);

        await Assert.ThrowsAsync<ApiUnauthorizedException>(() =>
            api.GetProfileAsync(accessToken: null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoginReturnsTheApiProblemForRejectedCredentials()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.Unauthorized,
            """{ "title": "Invalid email or password.", "status": 401 }""");
        var api = new BankingApiClient(httpClient);

        var result = await api.LoginAsync(
            new LoginRequest("alice@avenor.test", "wrong-password"), TestContext.Current.CancellationToken);

        Assert.Null(result.Value);
        Assert.Equal("Invalid email or password.", result.Problem!.Title);
    }
}
