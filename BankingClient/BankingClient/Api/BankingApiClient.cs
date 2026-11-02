using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Api;

/// <summary>
/// The client's only route to the API. Each method maps the status codes it expects to a
/// result, a 401 on a signed-in call to ApiUnauthorizedException, and anything else to an exception.
/// </summary>
public sealed class BankingApiClient(HttpClient httpClient) : IBankingApiClient
{
    // The API writes enums as names ("Checking", "Frozen").
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    #region Login
    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(new Uri("api/auth/login", UriKind.Relative), request, JsonOptions, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Success(await ReadAsync<LoginResponse>(response, cancellationToken)),
            HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized => Failure<LoginResponse>(await ReadProblemAsync(response, cancellationToken)),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Register
    public async Task<ApiProblem?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(new Uri("api/auth/register", UriKind.Relative), request, JsonOptions, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.Created => null,
            HttpStatusCode.BadRequest => await ReadProblemAsync(response, cancellationToken),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Profile
    public async Task<ProfileResponse> GetProfileAsync(string? accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("api/profile", UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        EnsureSessionAccepted(response);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw UnexpectedStatus(response);
        }

        return await ReadAsync<ProfileResponse>(response, cancellationToken);
    }
    #endregion

    #region Logout
    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(new Uri("api/auth/logout", UriKind.Relative), content: null, cancellationToken);

        // 401 means the token was already revoked or expired: the goal of logout is met.
        if (response.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.Unauthorized))
        {
            throw UnexpectedStatus(response);
        }
    }
    #endregion

    #region Get Accounts
    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(new Uri("api/accounts", UriKind.Relative), cancellationToken);
        EnsureSessionAccepted(response);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw UnexpectedStatus(response);
        }

        return await ReadAsync<List<AccountResponse>>(response, cancellationToken);
    }


    public async Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(new Uri($"api/accounts/{accountId}", UriKind.Relative), cancellationToken);
        EnsureSessionAccepted(response);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ReadAsync<AccountResponse>(response, cancellationToken),
            HttpStatusCode.NotFound => null,
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Get History
    public async Task<ApiResult<AccountHistoryResponse>> GetHistoryAsync(Guid accountId, DateOnly? from, DateOnly? onto, CancellationToken cancellationToken)
    {
        var query = string.Join('&', new[]
        {
            from is { } start ? string.Create(CultureInfo.InvariantCulture, $"from={start:yyyy-MM-dd}") : null,
            onto is { } end ? string.Create(CultureInfo.InvariantCulture, $"to={end:yyyy-MM-dd}") : null,
        }.OfType<string>());

        using var response = await httpClient.GetAsync(new Uri($"api/accounts/{accountId}/history?{query}", UriKind.Relative), cancellationToken);
        EnsureSessionAccepted(response);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Success(await ReadAsync<AccountHistoryResponse>(response, cancellationToken)),
            HttpStatusCode.BadRequest or HttpStatusCode.NotFound => Failure<AccountHistoryResponse>(await ReadProblemAsync(response, cancellationToken)),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Open Account
    public async Task<ApiResult<AccountResponse>> OpenAccountAsync(OpenAccountRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(new Uri("api/accounts", UriKind.Relative), request, JsonOptions, cancellationToken);
        EnsureSessionAccepted(response);

        return response.StatusCode switch
        {
            HttpStatusCode.Created => Success(await ReadAsync<AccountResponse>(response, cancellationToken)),
            HttpStatusCode.BadRequest => Failure<AccountResponse>(await ReadProblemAsync(response, cancellationToken)),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Move Money
    public Task<ApiResult<MoneyMovementResult>> DepositAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken) => SendMoneyMovementAsync(accountId, "deposits", request, idempotencyKey, cancellationToken);

    public Task<ApiResult<MoneyMovementResult>> WithdrawAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken) => SendMoneyMovementAsync(accountId, "withdrawals", request, idempotencyKey, cancellationToken);

    public Task<ApiResult<MoneyMovementResult>> TransferAsync(Guid fromAccountId, TransferMoneyRequest request, Guid idempotencyKey, CancellationToken cancellationToken) => SendMoneyMovementAsync(fromAccountId, "transfers", request, idempotencyKey, cancellationToken);

    // Deposits, withdrawals, and transfers share one contract: an Idempotency-Key header, 201 for
    // a new movement and 200 for a recognized retry, and problems for refused requests.
    private async Task<ApiResult<MoneyMovementResult>> SendMoneyMovementAsync<TBody>(Guid accountId, string operation, TBody body, Guid idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"api/accounts/{accountId}/{operation}", UriKind.Relative))
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        EnsureSessionAccepted(response);

        return response.StatusCode switch
        {
            HttpStatusCode.Created or HttpStatusCode.OK => Success(new MoneyMovementResult(await ReadAsync<LedgerEntryResponse>(response, cancellationToken), Replayed: response.StatusCode == HttpStatusCode.OK)),
            HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity => Failure<MoneyMovementResult>(await ReadProblemAsync(response, cancellationToken)),
            _ => throw UnexpectedStatus(response),
        };
    }

    #endregion

    #region Helpers
    // On a signed-in call, 401 means the session's token was revoked or expired.
    private static void EnsureSessionAccepted(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiUnauthorizedException();
        }
    }

    private static ApiResult<T> Success<T>(T value) where T : class => new(value, Problem: null);

    private static ApiResult<T> Failure<T>(ApiProblem problem) where T : class => new(Value: null, problem);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) => await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken) ?? throw new InvalidOperationException($"The API returned an empty {typeof(T).Name}.");

    private static async Task<ApiProblem> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions, cancellationToken);
        return new ApiProblem(problem?.Title ?? "The request could not be completed.", problem?.Detail, problem?.Errors.AsReadOnly() ?? new Dictionary<string, string[]>().AsReadOnly());
    }

    private static HttpRequestException UnexpectedStatus(HttpResponseMessage response) => new($"The API returned an unexpected {(int)response.StatusCode} for {response.RequestMessage?.RequestUri}.", inner: null, response.StatusCode);
    #endregion
}
