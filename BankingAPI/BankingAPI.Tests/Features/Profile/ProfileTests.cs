using System.Net;
using System.Net.Http.Json;

using BankingAPI.Features.Profile;
using BankingAPI.Identity;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace BankingAPI.Tests.Features.Profile;

public sealed class ProfileTests(BankingApiFactory factory)
{
    private static readonly Uri ProfileUri = new("/api/profile", UriKind.Relative);

    [Fact]
    public async Task ProfileReturnsSignedInUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);
        client.UseBearerToken(await client.GetAccessTokenAsync(email, cancellationToken));

        var profile = await client.GetFromJsonAsync<ProfileResponse>(ProfileUri, cancellationToken);

        Assert.Equal(email, profile!.Email);
        Assert.Contains(Roles.Customer, profile.Roles);
    }

    [Fact]
    public async Task ProfileRequiresAuthenticationThroughTheFallbackPolicy()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ProfileUri, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredTokenIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);

        // A second in-memory host whose clock is an hour behind issues a token
        // that expired 30 minutes ago; the normal host must refuse it.
        var anHourAgo = new FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(-1));
        await using var pastHost = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(anHourAgo)));
        using var pastClient = pastHost.CreateClient();
        client.UseBearerToken(await pastClient.GetAccessTokenAsync(email, cancellationToken));

        using var response = await client.GetAsync(ProfileUri, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
