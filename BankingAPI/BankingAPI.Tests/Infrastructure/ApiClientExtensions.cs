using System.Net.Http.Headers;
using System.Net.Http.Json;

using BankingAPI.Features.Auth;

namespace BankingAPI.Tests.Infrastructure;

internal static class ApiClientExtensions
{
    public const string TestPassword = "a-long-enough-password";

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.test";

    public static async Task<string> RegisterUserAsync(this HttpClient client, CancellationToken cancellationToken)
    {
        var email = UniqueEmail();
        using var response = await client.PostAsJsonAsync(new Uri("/api/auth/register", UriKind.Relative), new RegisterRequest(email, TestPassword, "Test User"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return email;
    }

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password, CancellationToken cancellationToken) => client.PostAsJsonAsync(new Uri("/api/auth/login", UriKind.Relative), new LoginRequest(email, password), cancellationToken);

    public static async Task<string> GetAccessTokenAsync(
        this HttpClient client, string email, CancellationToken cancellationToken)
    {
        using var response = await client.LoginAsync(email, TestPassword, cancellationToken);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        return login!.AccessToken;
    }

    public static async Task<HttpClient> CreateSignedInCustomerAsync(this BankingApiFactory factory, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);
        client.UseBearerToken(await client.GetAccessTokenAsync(email, cancellationToken));
        return client;
    }

    public static void UseBearerToken(this HttpClient client, string accessToken) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
