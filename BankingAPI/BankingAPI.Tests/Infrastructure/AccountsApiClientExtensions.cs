using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using BankingAPI.Domain.Accounts;
using BankingAPI.Features.Accounts;
using BankingAPI.Features.Auth;

namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// Accounts API calls for tests, using the API's JSON conventions (enums as names).
/// </summary>
internal static class AccountsApiClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task SignInAsync(this HttpClient client, string email, string password, CancellationToken cancellationToken)
    {
        using var response = await client.LoginAsync(email, password, cancellationToken);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        client.UseBearerToken(login!.AccessToken);
    }

    public static async Task<AccountResponse> OpenAccountAsync(this HttpClient client, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/accounts", UriKind.Relative), new OpenAccountRequest(AccountType.Checking, "Test Checking"), JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountResponse>(JsonOptions, cancellationToken))!;
    }

    /// <param name="operation">"deposits" or "withdrawals".</param>
    public static async Task<HttpResponseMessage> MoveMoneyAsync(
        this HttpClient client,
        Guid accountId,
        string operation,
        decimal amount,
        Guid? idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/accounts/{accountId}/{operation}", UriKind.Relative))
        {
            Content = JsonContent.Create(new MoneyMovementRequest(amount, Description: null), options: JsonOptions),
        };

        if (idempotencyKey is { } key)
        {
            request.Headers.Add("Idempotency-Key", key.ToString());
        }

        return await client.SendAsync(request, cancellationToken);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken) => (await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken))!;
}
