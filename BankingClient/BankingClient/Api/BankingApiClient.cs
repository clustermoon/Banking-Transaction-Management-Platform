using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Api;

/// <summary>
/// Provides the BankingClient's typed boundary to the Banking API.
/// Expected API responses are converted into result objects; unexpected responses
/// are surfaced as <see cref="HttpRequestException"/> instances.
/// </summary>
public sealed class BankingApiClient(HttpClient httpClient) : IBankingApiClient
{
    #region Login Async
    /// <summary>
    /// Attempts to authenticate a customer and returns either a session or API validation errors.
    /// </summary>
    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(new Uri("api/auth/login", UriKind.Relative), request, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => new ApiResult<LoginResponse>(await ReadAsync<LoginResponse>(response, cancellationToken), Problem: null),
            HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized => new ApiResult<LoginResponse>(Value: null, await ReadProblemAsync(response, cancellationToken)),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Register Async
    /// <summary>
    /// Creates a customer account.
    /// </summary>
    /// <remarks>
    /// A bad request is returned as an <see cref="ApiProblem"/> so the caller can
    /// display field-level validation messages without treating expected input errors
    /// as transport failures.
    /// </remarks>
    public async Task<ApiProblem?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(new Uri("api/auth/register", UriKind.Relative), request, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.Created => null,
            HttpStatusCode.BadRequest => await ReadProblemAsync(response, cancellationToken),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Get Profile Async
    /// <summary>
    /// Retrieves the authenticated customer's profile using the supplied bearer token.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Thrown when the API does not return the expected successful response.
    /// </exception>
    public async Task<ProfileResponse> GetProfileAsync(string? accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("api/profile", UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiUnauthorizedException();
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw UnexpectedStatus(response);
        }

        return await ReadAsync<ProfileResponse>(response, cancellationToken);
    }
    #endregion

    #region Logout Async
    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(new Uri("api/auth/logout", UriKind.Relative), content: null, cancellationToken);

        // Logout is idempotent from the client's perspective. If the API reports that
        // the token is already invalid, the user's intended end state has been achieved.
        if (response.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.Unauthorized))
        {
            throw UnexpectedStatus(response);
        }
    }
    #endregion

    #region Read Async
    // Treat a successful response with no body as a contract violation. Callers rely
    // on this method returning a complete API response object.
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new InvalidOperationException($"The API returned an empty {typeof(T).Name}.");
    #endregion

    #region Read Problem Async
    private static async Task<ApiProblem> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        return new ApiProblem(
            problem?.Title ?? "The request could not be completed.",
            problem?.Errors.AsReadOnly() ?? new Dictionary<string, string[]>().AsReadOnly()
        );
    }
    #endregion

    #region Unexpected Status
    private static HttpRequestException UnexpectedStatus(HttpResponseMessage response) => new
    (
        $"The API returned an unexpected {(int)response.StatusCode} for {response.RequestMessage?.RequestUri}.",
        inner: null,
        response.StatusCode
    );
    #endregion
}
