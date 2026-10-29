using System.Net;
using System.Net.Http.Json;

using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Tests.Features.Auth;

public sealed class LoginTests(BankingApiFactory factory)
{
    [Fact]
    public async Task LoginReturnsTokenForValidCredentials()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var response = await client.LoginAsync(email, ApiClientExtensions.TestPassword, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginRejectsWrongPassword()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var response = await client.LoginAsync(email, "wrong-password-here", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FiveFailedAttemptsLockTheAccountEvenForTheCorrectPassword()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.LoginAsync(email, "wrong-password-here", cancellationToken);
        }

        using var response = await client.LoginAsync(email, ApiClientExtensions.TestPassword, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("Account locked", problem!.Title);
    }
}
