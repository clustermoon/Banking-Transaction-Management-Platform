using System.Net;
using BankingAPI.Tests.Infrastructure;
namespace BankingAPI.Tests.Health;

public sealed class ReadinessTests(BankingApiFactory factory)
{
    [Fact]
    public async Task ReadyReturnsHealthyWhenDatabaseIsReachable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
