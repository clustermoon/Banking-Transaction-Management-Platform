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
/// A client for interacting with the banking API, providing methods for user authentication, account management, transaction history retrieval, and administrative operations. This class handles HTTP requests and responses, including error handling and session management, to facilitate communication between the banking client application and the banking API.
/// </summary>
/// <param name="httpClient"></param>
public sealed class BankingApiClient(HttpClient httpClient) : IBankingApiClient
{
    /// <summary>
    /// Serialization options for JSON content, configured to use web defaults and include a converter for string enums. This ensures that the API client can correctly serialize and deserialize JSON data when communicating with the banking API, handling enum values as strings in the JSON payloads.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    #region Login
    /// <summary>
    /// Logs in a user with the banking API, handling the request and response. This method sends a login request to the API and processes the response, returning either a successful result with the login response or a failure result with validation problems if the credentials are invalid. It also handles unexpected status codes by throwing an exception, indicating a potential issue with the API or the request.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Registers a new user with the banking API, handling the request and response. This method sends a registration request to the API and processes the response, returning an <see cref="ApiProblem"/> if there are validation errors or other issues. If the registration is successful, it returns null, indicating that the user has been created successfully.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Gets the profile information of the signed-in user, including their name, email, and account details. This method sends a request to the banking API to retrieve the profile data, ensuring that the session is accepted and handling any unexpected status codes. If the access token is provided, it is included in the request headers for authentication; otherwise, the method relies on the current session's authentication state.
    /// </summary>
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
    /// <summary>
    /// Logs the user out by sending a request to the banking API to invalidate the current session. This method handles the API response, ensuring that the logout operation is successful or that the session was already invalidated. If the response indicates an unexpected status code, it throws an exception to signal a potential issue with the API or the request.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Gets a list of all accounts for the signed-in user, including their current balance and associated user information. This method sends a request to the banking API to retrieve the account list, ensuring that the session is accepted and handling any unexpected status codes. The result is a read-only list of <see cref="AccountResponse"/> objects representing the user's accounts.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Gets the details of a specific account, including its current balance and associated user information. This method sends a request to the banking API to retrieve the account information, ensuring that the session is accepted and handling any unexpected status codes. If the account is not found, it returns null; otherwise, it returns an <see cref="AccountResponse"/> object containing the account details.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Opens a new bank account for the user, handling the request and response with the banking API. This method constructs the appropriate HTTP request for opening an account, sends it to the API, and processes the response. It returns either a successful result with the newly created account details or a failure result with validation problems if the request was invalid.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Deposits money into a specific account, handling the request and response with the banking API. This method constructs the appropriate HTTP request for a deposit operation, includes the idempotency key to prevent duplicate processing, and processes the API response. It returns either a successful result with the ledger entry or a failure result with validation problems.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="request"></param>
    /// <param name="idempotencyKey"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResult<MoneyMovementResult>> DepositAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken) => SendMoneyMovementAsync(accountId, "deposits", request, idempotencyKey, cancellationToken);

    /// <summary>
    /// Withdraws money from a specific account, handling the request and response with the banking API. This method constructs the appropriate HTTP request for a withdrawal operation, includes the idempotency key to prevent duplicate processing, and processes the API response. It returns either a successful result with the ledger entry or a failure result with validation problems.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="request"></param>
    /// <param name="idempotencyKey"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResult<MoneyMovementResult>> WithdrawAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken) => SendMoneyMovementAsync(accountId, "withdrawals", request, idempotencyKey, cancellationToken);

    /// <summary>
    /// Transfers money from one account to another, handling the request and response with the banking API. This method constructs the appropriate HTTP request for a transfer operation, includes the idempotency key to prevent duplicate processing, and processes the API response. It returns either a successful result with the ledger entry or a failure result with validation problems.
    /// </summary>
    /// <param name="fromAccountId"></param>
    /// <param name="request"></param>
    /// <param name="idempotencyKey"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResult<MoneyMovementResult>> TransferAsync(Guid fromAccountId, TransferMoneyRequest request, Guid idempotencyKey, CancellationToken cancellationToken) => SendMoneyMovementAsync(fromAccountId, "transfers", request, idempotencyKey, cancellationToken);

    /// <summary>
    /// Sends a money movement request to the banking API, handling deposits, withdrawals, and transfers. This method constructs the appropriate HTTP request based on the operation type, includes the idempotency key to prevent duplicate processing, and processes the API response. It returns either a successful result with the ledger entry or a failure result with validation problems.
    /// </summary>
    /// <typeparam name="TBody"></typeparam>
    /// <param name="accountId"></param>
    /// <param name="operation"></param>
    /// <param name="body"></param>
    /// <param name="idempotencyKey"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

    #region Get Admin Accounts
    /// <summary>
    /// Gets a list of all accounts for administrative purposes, including their current status and associated user information. This method sends a request to the banking API to retrieve the account list, ensuring that the session is accepted and handling any unexpected status codes. The result is a read-only list of <see cref="AdminAccountResponse"/> objects representing the accounts.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<AdminAccountResponse>> GetAdminAccountsAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(new Uri("api/admin/accounts", UriKind.Relative), cancellationToken);
        EnsureSessionAccepted(response);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw UnexpectedStatus(response);
        }

        return await ReadAsync<List<AdminAccountResponse>>(response, cancellationToken);
    }
    #endregion

    #region Get Admin Account
    /// <summary>
    /// Gets the details of a specific account for administrative purposes, including its current status and audit history. This method sends a request to the banking API to retrieve the account information, ensuring that the session is accepted and handling any unexpected status codes. If the account is not found, it returns null; otherwise, it returns an <see cref="AdminAccountResponse"/> object containing the account details.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AdminAccountResponse?> GetAdminAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            new Uri($"api/admin/accounts/{accountId}", UriKind.Relative), cancellationToken);
        EnsureSessionAccepted(response);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ReadAsync<AdminAccountResponse>(response, cancellationToken),
            HttpStatusCode.NotFound => null,
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Get Audit Log
    /// <summary>
    /// Gets the audit log of account status changes, optionally filtered by a specific account ID. This method sends a request to the banking API to retrieve the audit log entries, ensuring that the session is accepted and handling any unexpected status codes. The result is a read-only list of <see cref="AuditEntryResponse"/> objects representing the audit log entries.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<AuditEntryResponse>> GetAuditLogAsync(Guid? accountId, CancellationToken cancellationToken)
    {
        var path = accountId is { } id ? $"api/admin/audit?accountId={id}" : "api/admin/audit";
        using var response = await httpClient.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
        EnsureSessionAccepted(response);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw UnexpectedStatus(response);
        }

        return await ReadAsync<List<AuditEntryResponse>>(response, cancellationToken);
    }
    #endregion

    #region Change Account Status
    /// <summary>
    /// Changes the status of a specific account, allowing an admin to freeze, unfreeze, or close an account. This method sends a request to the banking API with the desired status change and the reason for the change. It handles the API response, returning either the resulting audit entry if successful or an <see cref="ApiProblem"/> if there were validation errors or other issues. The method also ensures that the session is accepted and throws exceptions for unexpected status codes.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="toStatus"></param>
    /// <param name="reason"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<ApiResult<AuditEntryResponse>> ChangeAccountStatusAsync(
        Guid accountId, AccountStatus toStatus, string reason, CancellationToken cancellationToken)
    {
        var action = toStatus switch
        {
            AccountStatus.Frozen => "freeze",
            AccountStatus.Active => "unfreeze",
            AccountStatus.Closed => "close",
            _ => throw new ArgumentOutOfRangeException(nameof(toStatus), toStatus, "Unsupported target status."),
        };

        using var response = await httpClient.PostAsJsonAsync(
            new Uri($"api/admin/accounts/{accountId}/{action}", UriKind.Relative),
            new StatusChangeRequest(reason),
            JsonOptions,
            cancellationToken);
        EnsureSessionAccepted(response);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => Success(await ReadAsync<AuditEntryResponse>(response, cancellationToken)),
            HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity =>
                Failure<AuditEntryResponse>(await ReadProblemAsync(response, cancellationToken)),
            _ => throw UnexpectedStatus(response),
        };
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Ensures that the API response indicates an accepted session. If the response status code is 401 Unauthorized, it throws an <see cref="ApiUnauthorizedException"/>, indicating that the user's session is no longer valid and they need to re-authenticate. This method is used to handle cases where the API refuses the session's token, ensuring that the client can respond appropriately by signing the user out and prompting for login again.
    /// </summary>
    /// <param name="response"></param>
    /// <exception cref="ApiUnauthorizedException"></exception>
    private static void EnsureSessionAccepted(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiUnauthorizedException();
        }
    }

    /// <summary>
    /// Success result for an API call, containing the value returned by the API. This is used to represent cases where the API call was successful and returned the expected data, allowing the client to handle and process the result appropriately.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    private static ApiResult<T> Success<T>(T value) where T : class => new(value, Problem: null);

    /// <summary>
    /// Failure result for an API call, containing the problem details returned by the API. This is used to represent cases where the API call was unsuccessful due to validation errors or other issues, allowing the client to handle and display the problem information appropriately.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="problem"></param>
    /// <returns></returns>
    private static ApiResult<T> Failure<T>(ApiProblem problem) where T : class => new(Value: null, problem);

    /// <summary>
    /// Reads a JSON response from the API and deserializes it into an object of type <typeparamref name="T"/>. If the response content is empty, throws an <see cref="InvalidOperationException"/> indicating that the API returned an unexpected empty response. This method is used to handle successful API responses that return data in JSON format.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="response"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) => await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken) ?? throw new InvalidOperationException($"The API returned an empty {typeof(T).Name}.");

    /// <summary>
    /// Reads a validation problem from the API response and converts it into an <see cref="ApiProblem"/> object. This method is used to handle cases where the API returns a 400 Bad Request or other validation-related status codes, allowing the client to extract and present the validation errors to the user.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task<ApiProblem> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions, cancellationToken);
        return new ApiProblem(problem?.Title ?? "The request could not be completed.", problem?.Detail, problem?.Errors.AsReadOnly() ?? new Dictionary<string, string[]>().AsReadOnly());
    }

    /// <summary>
    /// Unexpected status codes are a sign of a bug in the API or the client. The API should never return a 500, 404, or 400 for a request that is known to be valid. The client should never send a request that is known to be invalid. If either happens, it's a bug that needs to be fixed.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    private static HttpRequestException UnexpectedStatus(HttpResponseMessage response) => new($"The API returned an unexpected {(int)response.StatusCode} for {response.RequestMessage?.RequestUri}.", inner: null, response.StatusCode);
    #endregion
}
