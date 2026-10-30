using System.Net;
using System.Net.Http.Json;

using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Tests.Features.Auth;

public sealed class LoginTests(BankingApiFactory factory)
{
    #region FACT? - Login Returns Token For Valid Credentials
    [Fact]
    public async Task LoginReturnsTokenForValidCredentials()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var response = await client.LoginAsync(email, ApiClientExtensions.TestPassword, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    #endregion

    #region FACT? - Login Rejects Wrong Password
    [Fact]
    public async Task LoginRejectsWrongPassword()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        using var response = await client.LoginAsync(email, "wrong-password-here", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    #endregion

    #region FACT? - Five Failed Attempts Lock The Account Even For The Correct Password
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
    #endregion
}
