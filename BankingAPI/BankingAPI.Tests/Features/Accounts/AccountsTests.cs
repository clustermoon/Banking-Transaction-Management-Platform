using System.Net;
using System.Net.Http.Json;

using BankingAPI.Data;
using BankingAPI.Data.Seeding;
using BankingAPI.Domain.Accounts;
using BankingAPI.Features.Accounts;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Features.Accounts;

public sealed class AccountsTests(BankingApiFactory factory)
{
    #region FACT? - Opened Account Appears In The Owners List
    [Fact]
    public async Task OpenedAccountAppearsInTheOwnersList()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);

        var opened = await client.OpenAccountAsync(cancellationToken);
        var accounts = await client.GetFromJsonAsync<List<AccountResponse>>(
            "/api/accounts", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        var listed = Assert.Single(accounts!);
        Assert.Equal(opened.Id, listed.Id);
        Assert.Equal(AccountStatus.Active, listed.Status);
        Assert.Equal(0m, listed.Balance);
    }
    #endregion

    #region THEORY? - Another Customer's Account Is Not Found
    [Theory]
    [InlineData("GET", "")]
    [InlineData("GET", "/history")]
    [InlineData("POST", "/deposits")]
    public async Task AnotherCustomersAccountIsNotFound(string method, string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var aliceAccountId = await GetDemoAccountIdAsync(DemoBankingData.AliceCheckingNumber, cancellationToken);
        using var ben = factory.CreateClient();
        await ben.SignInAsync(DemoCredentials.BenEmail, DemoCredentials.CustomerPassword, cancellationToken);

        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri($"/api/accounts/{aliceAccountId}{path}", UriKind.Relative));
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new MoneyMovementRequest(10m, null));
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        }

        using var response = await ben.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    #endregion

    #region FACT? - List Contains Only Your Own Accounts
    [Fact]
    public async Task ListContainsOnlyYourOwnAccounts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var ben = factory.CreateClient();
        await ben.SignInAsync(DemoCredentials.BenEmail, DemoCredentials.CustomerPassword, cancellationToken);

        var accounts = await ben.GetFromJsonAsync<List<AccountResponse>>(
            "/api/accounts", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        var only = Assert.Single(accounts!);
        Assert.Equal(DemoBankingData.BenCheckingNumber, only.AccountNumber);
    }
    #endregion

    #region FACT? - Deposits And Withdrawals Appear In History
    [Fact]
    public async Task DepositsAndWithdrawalsAppearInHistory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var account = await client.OpenAccountAsync(cancellationToken);

        using var deposit = await client.MoveMoneyAsync(account.Id, "deposits", 100m, Guid.NewGuid(), cancellationToken);
        using var withdrawal = await client.MoveMoneyAsync(account.Id, "withdrawals", 40m, Guid.NewGuid(), cancellationToken);
        var history = await client.GetFromJsonAsync<AccountHistoryResponse>(
            $"/api/accounts/{account.Id}/history", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, deposit.StatusCode);
        Assert.Equal(HttpStatusCode.Created, withdrawal.StatusCode);
        Assert.Equal([-40m, 100m], history!.Entries.Select(entry => entry.SignedAmount));
        Assert.Equal(0m, history.OpeningBalance);
        Assert.Equal(60m, history.ClosingBalance);
    }
    #endregion

    #region FACT? - Replayed Deposit Returns The Original And Moves Money Once
    [Fact]
    public async Task ReplayedDepositReturnsTheOriginalAndMovesMoneyOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var account = await client.OpenAccountAsync(cancellationToken);
        var key = Guid.NewGuid();

        using var first = await client.MoveMoneyAsync(account.Id, "deposits", 75m, key, cancellationToken);
        using var retry = await client.MoveMoneyAsync(account.Id, "deposits", 75m, key, cancellationToken);
        var refreshed = await client.GetFromJsonAsync<AccountResponse>(
            $"/api/accounts/{account.Id}", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(
            (await first.ReadAsync<LedgerEntryResponse>(cancellationToken)).Id,
            (await retry.ReadAsync<LedgerEntryResponse>(cancellationToken)).Id);
        Assert.Equal(75m, refreshed!.Balance);
    }
    #endregion

    #region FACT? - Money Movement Without An Idempotency Key Is Rejected
    [Fact]
    public async Task MoneyMovementWithoutAnIdempotencyKeyIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var account = await client.OpenAccountAsync(cancellationToken);

        using var response = await client.MoveMoneyAsync(account.Id, "deposits", 10m, idempotencyKey: null, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    #endregion

    #region FACT? - Withdrawal Beyond The Balance Is Unprocessable
    [Fact]
    public async Task WithdrawalBeyondTheBalanceIsUnprocessable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var account = await client.OpenAccountAsync(cancellationToken);

        using var response = await client.MoveMoneyAsync(account.Id, "withdrawals", 0.01m, Guid.NewGuid(), cancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("Insufficient funds for this withdrawal.", problem.Detail);
    }
    #endregion

    #region FACT? - Statement Opening Plus Movements Equals Closing
    [Fact]
    public async Task StatementOpeningPlusMovementsEqualsClosing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var aliceAccountId = await GetDemoAccountIdAsync(DemoBankingData.AliceCheckingNumber, cancellationToken);
        using var alice = factory.CreateClient();
        await alice.SignInAsync(DemoCredentials.AliceEmail, DemoCredentials.CustomerPassword, cancellationToken);

        var statement = await alice.GetFromJsonAsync<AccountHistoryResponse>($"/api/accounts/{aliceAccountId}/history", AccountsApiClientExtensions.JsonOptions, cancellationToken);
        var account = await alice.GetFromJsonAsync<AccountResponse>($"/api/accounts/{aliceAccountId}", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        Assert.NotEmpty(statement!.Entries);
        Assert.Equal(statement.ClosingBalance, statement.OpeningBalance + statement.Entries.Sum(entry => entry.SignedAmount));
        Assert.Equal(account!.Balance, statement.ClosingBalance);
    }
    #endregion

    #region FACT? - Transfer Between Your Own Accounts Shows In Both Histories
    [Fact]
    public async Task TransferBetweenYourOwnAccountsShowsInBothHistories()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var checking = await client.OpenAccountAsync(cancellationToken);
        var savings = await client.OpenAccountAsync(cancellationToken);
        using var deposit = await client.MoveMoneyAsync(checking.Id, "deposits", 100m, Guid.NewGuid(), cancellationToken);

        using var transfer = await client.TransferAsync(checking.Id, savings.AccountNumber, 40m, Guid.NewGuid(), cancellationToken);
        var sent = await transfer.ReadAsync<LedgerEntryResponse>(cancellationToken);
        var savingsHistory = await client.GetFromJsonAsync<AccountHistoryResponse>($"/api/accounts/{savings.Id}/history", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, transfer.StatusCode);
        Assert.Equal(-40m, sent.SignedAmount);
        Assert.Equal(60m, sent.BalanceAfter);
        var received = Assert.Single(savingsHistory!.Entries);
        Assert.Equal(40m, received.SignedAmount);
    }
    #endregion

    #region FACT? - Transfer To Another Customer Reveals Only Masked Numbers
    [Fact]
    public async Task TransferToAnotherCustomerRevealsOnlyMaskedNumbers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var sender = await SignedInNewCustomerAsync(cancellationToken);
        using var receiver = await SignedInNewCustomerAsync(cancellationToken);
        var from = await sender.OpenAccountAsync(cancellationToken);
        var to = await receiver.OpenAccountAsync(cancellationToken);
        using var deposit = await sender.MoveMoneyAsync(from.Id, "deposits", 100m, Guid.NewGuid(), cancellationToken);

        using var transfer = await sender.TransferAsync(from.Id, to.AccountNumber, 25m, Guid.NewGuid(), cancellationToken);
        var sent = await transfer.ReadAsync<LedgerEntryResponse>(cancellationToken);
        var receiverHistory = await receiver.GetFromJsonAsync<AccountHistoryResponse>($"/api/accounts/{to.Id}/history", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        // Each side sees the other only as the last four digits, never the account's name.
        Assert.Equal($"Transfer to •••• {to.AccountNumber[^4..]}", sent.Description);
        Assert.Equal($"Transfer from •••• {from.AccountNumber[^4..]}", Assert.Single(receiverHistory!.Entries).Description);
    }
    #endregion

    #region FACT? - Replayed Transfer Returns The Original
    [Fact]
    public async Task ReplayedTransferReturnsTheOriginal()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var checking = await client.OpenAccountAsync(cancellationToken);
        var savings = await client.OpenAccountAsync(cancellationToken);
        using var deposit = await client.MoveMoneyAsync(checking.Id, "deposits", 100m, Guid.NewGuid(), cancellationToken);
        var key = Guid.NewGuid();

        using var first = await client.TransferAsync(checking.Id, savings.AccountNumber, 30m, key, cancellationToken);
        using var retry = await client.TransferAsync(checking.Id, savings.AccountNumber, 30m, key, cancellationToken);
        var refreshed = await client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{checking.Id}", AccountsApiClientExtensions.JsonOptions, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(70m, refreshed!.Balance);
    }
    #endregion

    #region FACT - Malformed Account Number Is Rejected With A Field Error
    [Fact]
    public async Task MalformedAccountNumberIsRejectedWithAFieldError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var account = await client.OpenAccountAsync(cancellationToken);

        using var response = await client.TransferAsync(account.Id, "12AB", 10m, Guid.NewGuid(), cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>(cancellationToken);
        Assert.Contains(problem.Errors.Keys, key => key.Equals("ToAccountNumber", StringComparison.OrdinalIgnoreCase));
    }
    #endregion

    #region FACT? - Unknown Destination Is Unprocessable
    [Fact]
    public async Task UnknownDestinationIsUnprocessable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await SignedInNewCustomerAsync(cancellationToken);
        var account = await client.OpenAccountAsync(cancellationToken);
        using var deposit = await client.MoveMoneyAsync(account.Id, "deposits", 50m, Guid.NewGuid(), cancellationToken);

        using var response = await client.TransferAsync(account.Id, "0000000000", 10m, Guid.NewGuid(), cancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>(cancellationToken);
        Assert.Equal("We couldn't find an active account with that number.", problem.Detail);
    }
    #endregion

    #region FACT? - Transfer From Another Customer's Account Is Not Found
    [Fact]
    public async Task TransferFromAnotherCustomersAccountIsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var aliceAccountId = await GetDemoAccountIdAsync(DemoBankingData.AliceCheckingNumber, cancellationToken);
        using var ben = factory.CreateClient();
        await ben.SignInAsync(DemoCredentials.BenEmail, DemoCredentials.CustomerPassword, cancellationToken);

        // Ben names Alice's account as the source and his own as the destination.
        using var response = await ben.TransferAsync(aliceAccountId, DemoBankingData.BenCheckingNumber, 10m, Guid.NewGuid(), cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    #endregion

    #region Helpers
    private async Task<HttpClient> SignedInNewCustomerAsync(CancellationToken cancellationToken)
    {
        var client = factory.CreateClient();
        var email = await client.RegisterUserAsync(cancellationToken);
        client.UseBearerToken(await client.GetAccessTokenAsync(email, cancellationToken));
        return client;
    }

    private async Task<Guid> GetDemoAccountIdAsync(string accountNumber, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        return await dbContext.Accounts.AsNoTracking().Where(account => account.AccountNumber == accountNumber).Select(account => account.Id).SingleAsync(cancellationToken);
    }
    #endregion
}
