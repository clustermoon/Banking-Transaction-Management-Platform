using System.Net;
using System.Net.Http.Json;

using BankingAPI.Data.Seeding;
using BankingAPI.Domain.Accounts;
using BankingAPI.Features.Accounts;
using BankingAPI.Features.Admin;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Tests.Features.Admin;

public sealed class AdminTests(BankingApiFactory factory)
{
    #region THEORY? - Customers Are Forbidden From Admin Endpoints
    [Theory]
    [InlineData("GET", "/api/admin/accounts")]
    [InlineData("GET", "/api/admin/audit")]
    [InlineData("POST", "/api/admin/accounts/00000000-0000-0000-0000-000000000001/freeze")]
    public async Task CustomersAreForbiddenFromAdminEndpoints(string method, string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var ben = factory.CreateClient();
        await ben.SignInAsync(DemoCredentials.BenEmail, DemoCredentials.CustomerPassword, cancellationToken);

        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new StatusChangeRequest("Test"));
        }

        using var response = await ben.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    #endregion

    #region FACT? - Admins Are Forbidden From Personal Banking
    [Fact]
    public async Task AdminsAreForbiddenFromPersonalBanking()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var admin = await SignedInAdminAsync(cancellationToken);

        using var response = await admin.GetAsync(new Uri("/api/accounts", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    #endregion

    #region FACT? - Admin Sees Every Account With Its Owner
    [Fact]
    public async Task AdminSeesEveryAccountWithItsOwner()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var admin = await SignedInAdminAsync(cancellationToken);

        var accounts = await admin.GetFromJsonAsync<List<AdminAccountResponse>>("/api/admin/accounts", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        var aliceChecking = Assert.Single(accounts!, account => account.AccountNumber == DemoBankingData.AliceCheckingNumber);
        Assert.Equal("Alice Morgan", aliceChecking.OwnerName);
        Assert.Equal(DemoCredentials.AliceEmail, aliceChecking.OwnerEmail);
    }
    #endregion

    #region FACT? - Freeze Is Audited And Stops Money Moving
    [Fact]
    public async Task FreezeIsAuditedAndStopsMoneyMoving()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await factory.CreateSignedInCustomerAsync(cancellationToken);
        var account = await customer.OpenAccountAsync(cancellationToken);
        using var admin = await SignedInAdminAsync(cancellationToken);

        using var freeze = await admin.PostAsJsonAsync(new Uri($"/api/admin/accounts/{account.Id}/freeze", UriKind.Relative), new StatusChangeRequest("Suspicious activity"), cancellationToken);
        var entry = await freeze.ReadAsync<AuditEntryResponse>(cancellationToken);
        var log = await admin.GetFromJsonAsync<List<AuditEntryResponse>>($"/api/admin/audit?accountId={account.Id}", AccountsApiClientExtensions.JsonOptions, cancellationToken);
        using var deposit = await customer.MoveMoneyAsync(account.Id, "deposits", 10m, Guid.NewGuid(), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, freeze.StatusCode);
        Assert.Equal(DemoCredentials.AdminDisplayName, entry.ActorName);
        Assert.Equal((AccountStatus.Active, AccountStatus.Frozen), (entry.FromStatus, entry.ToStatus));
        Assert.Equal("Suspicious activity", entry.Reason);
        Assert.Equal(entry.Id, Assert.Single(log!).Id);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, deposit.StatusCode);
    }
    #endregion

    #region FACT? - Status Change Without A Reason Is Rejected
    [Fact]
    public async Task StatusChangeWithoutAReasonIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await factory.CreateSignedInCustomerAsync(cancellationToken);
        var account = await customer.OpenAccountAsync(cancellationToken);
        using var admin = await SignedInAdminAsync(cancellationToken);

        using var response = await admin.PostAsJsonAsync(new Uri($"/api/admin/accounts/{account.Id}/freeze", UriKind.Relative), new StatusChangeRequest(""), cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>(cancellationToken);
        Assert.Contains(problem.Errors.Keys, key => key.Equals("Reason", StringComparison.OrdinalIgnoreCase));
    }
    #endregion

    #region FACT? - Closing An Account That Holds Money Is Unprocessable
    [Fact]
    public async Task ClosingAnAccountThatHoldsMoneyIsUnprocessable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await factory.CreateSignedInCustomerAsync(cancellationToken);
        var account = await customer.OpenAccountAsync(cancellationToken);
        using var deposit = await customer.MoveMoneyAsync(account.Id, "deposits", 10m, Guid.NewGuid(), cancellationToken);
        using var admin = await SignedInAdminAsync(cancellationToken);

        using var response = await admin.PostAsJsonAsync(new Uri($"/api/admin/accounts/{account.Id}/close", UriKind.Relative), new StatusChangeRequest("Customer request"), cancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("An account can only be closed once its balance is zero.", problem.Detail);
    }
    #endregion

    #region FACT? - Simultaneous Freezes Record Exactly One Change
    [Fact]
    public async Task SimultaneousFreezesRecordExactlyOneChange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var customer = await factory.CreateSignedInCustomerAsync(cancellationToken);
        var account = await customer.OpenAccountAsync(cancellationToken);
        using var admin = await SignedInAdminAsync(cancellationToken);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => admin.PostAsJsonAsync(new Uri($"/api/admin/accounts/{account.Id}/freeze", UriKind.Relative), new StatusChangeRequest("Simultaneous review"), cancellationToken)));
        var log = await admin.GetFromJsonAsync<List<AuditEntryResponse>>($"/api/admin/audit?accountId={account.Id}", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.OK), response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity }));
        Assert.Single(log!);

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
    #endregion

    #region Helpers
    private async Task<HttpClient> SignedInAdminAsync(CancellationToken cancellationToken)
    {
        var client = factory.CreateClient();
        await client.SignInAsync(DemoCredentials.AdminEmail, DemoCredentials.AdminPassword, cancellationToken);
        return client;
    }
    #endregion
}
