using System.Net;

using BankingClient.Api;
using BankingClient.Api.Contracts;
using BankingClient.Tests.Infrastructure;

namespace BankingClient.Tests.Api;

/// <summary>
/// This class contains unit tests for the BankingApiClient, which is responsible for interacting with the banking API. The tests cover various scenarios, including handling unauthorized responses, validating API problem responses, ensuring idempotency in deposit operations, and verifying that account status changes are sent to the correct API endpoints. These tests help ensure that the BankingApiClient behaves correctly under different conditions and handles API responses as expected.
/// </summary>
public sealed class BankingApiClientTests
{
    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string? json = null) => new(new StubHttpMessageHandler(statusCode, json)) { BaseAddress = new Uri("http://api.test/") };
    private const string EntryJson = """ { "id": 7, "occurredAt": "2026-10-01T12:00:00+00:00", "type": "Deposit", "description": "Online deposit", "signedAmount": 25.00, "balanceAfter": 25.00 } """;

    #region FACT? - Get Profile Throws Unauthorized When The Api Rejects The Token
    [Fact]
    public async Task GetProfileThrowsUnauthorizedWhenTheApiRejectsTheToken()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.Unauthorized);
        var api = new BankingApiClient(httpClient);

        await Assert.ThrowsAsync<ApiUnauthorizedException>(() => api.GetProfileAsync(accessToken: null, TestContext.Current.CancellationToken));
    }
    #endregion

    #region FACT? - Login Returns The Api Problem For Rejected Credentials
    [Fact]
    public async Task LoginReturnsTheApiProblemForRejectedCredentials()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.Unauthorized, """{ "title": "Invalid email or password.", "status": 401 }""");
        var api = new BankingApiClient(httpClient);

        var result = await api.LoginAsync(new LoginRequest("alice@avenor.test", "wrong-password"), TestContext.Current.CancellationToken);

        Assert.Null(result.Value);
        Assert.Equal("Invalid email or password.", result.Problem!.Title);
    }
    #endregion

    #region FACT? - Deposit Sends The Forms Idempotency Key
    [Fact]
    public async Task DepositSendsTheFormsIdempotencyKey()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Created, EntryJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var api = new BankingApiClient(httpClient);
        var key = Guid.NewGuid();

        var result = await api.DepositAsync(Guid.NewGuid(), new MoneyMovementRequest(25m, null), key, TestContext.Current.CancellationToken);

        Assert.Equal(key.ToString(), Assert.Single(handler.LastRequest!.Headers.GetValues("Idempotency-Key")));
        Assert.False(result.Value!.Replayed);
    }
    #endregion

    #region FACT? - Repeated Deposit Is Reported As Replayed
    [Fact]
    public async Task RepeatedDepositIsReportedAsReplayed()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.OK, EntryJson);
        var api = new BankingApiClient(httpClient);

        var result = await api.DepositAsync(Guid.NewGuid(), new MoneyMovementRequest(25m, null), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.True(result.Value!.Replayed);
    }
    #endregion

    #region FACT? - Broken Business Rule Keeps Its Message
    [Fact]
    public async Task BrokenBusinessRuleKeepsItsMessage()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.UnprocessableEntity, """{ "title": "Request could not be completed", "detail": "Insufficient funds for this withdrawal.", "status": 422 }""");
        var api = new BankingApiClient(httpClient);

        var result = await api.WithdrawAsync(Guid.NewGuid(), new MoneyMovementRequest(500m, null), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal("Insufficient funds for this withdrawal.", result.Problem!.Detail);
    }
    #endregion

    #region FACT? - Another Customers Account Comes Back As NotFound
    [Fact]
    public async Task AnotherCustomersAccountComesBackAsNotFound()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.NotFound);
        var api = new BankingApiClient(httpClient);

        var account = await api.GetAccountAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(account);
    }
    #endregion

    #region FACT? - Transfer Is Sent As An Action On The Source Account
    [Fact]
    public async Task TransferIsSentAsAnActionOnTheSourceAccount()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Created, EntryJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var api = new BankingApiClient(httpClient);
        var sourceId = Guid.NewGuid();
        var key = Guid.NewGuid();

        await api.TransferAsync(
            sourceId, new TransferMoneyRequest("7301002457", 10m, null), key, TestContext.Current.CancellationToken);

        Assert.Equal($"/api/accounts/{sourceId}/transfers", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(key.ToString(), Assert.Single(handler.LastRequest.Headers.GetValues("Idempotency-Key")));
    }
    #endregion

    #region THEORY? - Status Change Is Sent To The Matching Action
    [Theory]
    [InlineData(AccountStatus.Frozen, "freeze")]
    [InlineData(AccountStatus.Active, "unfreeze")]
    [InlineData(AccountStatus.Closed, "close")]
    public async Task StatusChangeIsSentToTheMatchingAction(AccountStatus toStatus, string action)
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.UnprocessableEntity, """{ "title": "Refused" }""");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        var api = new BankingApiClient(httpClient);
        var accountId = Guid.NewGuid();

        await api.ChangeAccountStatusAsync(accountId, toStatus, "Test", TestContext.Current.CancellationToken);

        Assert.Equal($"/api/admin/accounts/{accountId}/{action}", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
    #endregion

    #region FACT? - Losing A Race To Another Admin Comes Back As A Problem
    [Fact]
    public async Task LosingARaceToAnotherAdminComesBackAsAProblem()
    {
        using var httpClient = CreateHttpClient(
            HttpStatusCode.Conflict,
            """{ "title": "Account changed", "detail": "This account was changed by someone else. Reload it and try again.", "status": 409 }""");
        var api = new BankingApiClient(httpClient);

        var result = await api.ChangeAccountStatusAsync(
            Guid.NewGuid(), AccountStatus.Frozen, "Test", TestContext.Current.CancellationToken);

        Assert.Equal("This account was changed by someone else. Reload it and try again.", result.Problem!.Detail);
    }
    #endregion
}
