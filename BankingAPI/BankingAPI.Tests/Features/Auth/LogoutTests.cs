using System.Net;

using BankingAPI.Tests.Infrastructure;

namespace BankingAPI.Tests.Features.Auth;

public sealed class LogoutTests(BankingApiFactory factory)
{
    private static readonly Uri LogoutUri = new("/api/auth/logout", UriKind.Relative);
    private static readonly Uri ProfileUri = new("/api/profile", UriKind.Relative);

    [Fact]
    public async Task TokenIsRejectedAfterLogout()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);
        client.UseBearerToken(await client.GetAccessTokenAsync(email, cancellationToken));

        using var beforeLogout = await client.GetAsync(ProfileUri, cancellationToken);
        using var logout = await client.PostAsync(LogoutUri, content: null, cancellationToken);
        using var afterLogout = await client.GetAsync(ProfileUri, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, beforeLogout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task SigningInAgainAfterLogoutIssuesAWorkingToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);
        client.UseBearerToken(await client.GetAccessTokenAsync(email, cancellationToken));
        using var logout = await client.PostAsync(LogoutUri, content: null, cancellationToken);

        client.UseBearerToken(await client.GetAccessTokenAsync(email, cancellationToken));
        using var response = await client.GetAsync(ProfileUri, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
