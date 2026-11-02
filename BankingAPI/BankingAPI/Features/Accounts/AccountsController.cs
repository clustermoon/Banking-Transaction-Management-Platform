using System.ComponentModel.DataAnnotations;

using BankingAPI.Data;
using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Ledger;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Features.Accounts;

/// <summary>
/// A customer's own accounts. Reads query the database directly; writes go through the domain:
/// Account.Open for new accounts and LedgerService for every money movement. Another customer's
/// account is always reported as not found, so its existence is never confirmed.
/// </summary>
[ApiController]
[Route("api/accounts")]
[Authorize(Policy = Policies.Customer)]
public sealed class AccountsController(
    BankingDbContext dbContext,
    LedgerService ledger,
    TimeProvider timeProvider) : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    #region GET list accounts
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AccountResponse>> List(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var accounts = await dbContext.Accounts.AsNoTracking().Where(account => account.OwnerId == userId).OrderBy(account => account.OpenedAt).ToListAsync(cancellationToken);

        return [.. accounts.Select(AccountResponse.From)];
    }
    #endregion

    #region GET get account
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<AccountResponse> Get(Guid id, CancellationToken cancellationToken) => AccountResponse.From(await FindOwnedAccountAsync(id, cancellationToken));
    #endregion

    #region POST Open
    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(OpenAccountRequest request, CancellationToken cancellationToken)
    {
        var account = Account.Open(User.GetUserId(), request.Type, request.Name, AccountNumberGenerator.Generate(), timeProvider.GetUtcNow());
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = account.Id }, AccountResponse.From(account));
    }
    #endregion

    #region GET History
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<AccountHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<AccountHistoryResponse> History(Guid id, [FromQuery] HistoryQuery query, CancellationToken cancellationToken)
    {
        await FindOwnedAccountAsync(id, cancellationToken);

        var to = query.To ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = query.From ?? to.AddDays(-29);
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // An account's entries are recorded in time order, so Id order is time order: the last
        // entry before the range holds the opening balance.
        var openingBalance = await dbContext.LedgerEntries.AsNoTracking().Where(entry => entry.AccountId == id && entry.OccurredAt < start).OrderByDescending(entry => entry.Id).Select(entry => (decimal?)entry.BalanceAfter).FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var entries = await dbContext.LedgerEntries.AsNoTracking().Where(entry => entry.AccountId == id && entry.OccurredAt >= start && entry.OccurredAt < end).OrderByDescending(entry => entry.Id).ToListAsync(cancellationToken);

        var closingBalance = entries.Count > 0 ? entries[0].BalanceAfter : openingBalance;

        return new AccountHistoryResponse(from, to, openingBalance, closingBalance, [.. entries.Select(LedgerEntryResponse.From)]);
    }
    #endregion

    #region POST Deposits
    [HttpPost("{id:guid}/deposits")]
    [ProducesResponseType<LedgerEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<LedgerEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> Deposit(Guid id, MoneyMovementRequest request, [FromHeader(Name = IdempotencyKeyHeader), Required] Guid? idempotencyKey, CancellationToken cancellationToken) => MoveMoneyAsync(ledger.DepositAsync, id, request, idempotencyKey!.Value, "Online deposit", cancellationToken);
    #endregion

    #region POST Withdrawals
    [HttpPost("{id:guid}/withdrawals")]
    [ProducesResponseType<LedgerEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<LedgerEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> Withdraw(Guid id, MoneyMovementRequest request, [FromHeader(Name = IdempotencyKeyHeader), Required] Guid? idempotencyKey, CancellationToken cancellationToken) => MoveMoneyAsync(ledger.WithdrawAsync, id, request, idempotencyKey!.Value, "Online withdrawal", cancellationToken);
    #endregion

    #region POST Transfers
    [HttpPost("{id:guid}/transfers")]
    [ProducesResponseType<LedgerEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<LedgerEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer(
    Guid id,
    TransferMoneyRequest request,
    [FromHeader(Name = IdempotencyKeyHeader), Required] Guid? idempotencyKey,
    CancellationToken cancellationToken)
    {
        var result = await ledger.TransferAsync(
            new TransferRequest(User.GetUserId(), id, request.ToAccountNumber, request.Amount, request.Note, idempotencyKey!.Value),
            cancellationToken);

        return ToResponse(result);
    }
    #endregion

    #region Helpers
    // A new movement is 201 Created; a replayed retry returns the original entry with 200 OK.
    private async Task<IActionResult> MoveMoneyAsync(
        Func<LedgerRequest, CancellationToken, Task<LedgerOperationResult>> operation,
        Guid accountId,
        MoneyMovementRequest request,
        Guid idempotencyKey,
        string defaultDescription,
        CancellationToken cancellationToken)
    {
        var description = string.IsNullOrWhiteSpace(request.Description) ? defaultDescription : request.Description.Trim();
        var result = await operation(new LedgerRequest(User.GetUserId(), accountId, request.Amount, description, idempotencyKey), cancellationToken);

        return ToResponse(result);
    }

    // A new movement is 201 Created; a replayed retry returns the original entry with 200 OK.
    private IActionResult ToResponse(LedgerOperationResult result)
    {
        var body = LedgerEntryResponse.From(result.Entry);
        return result.Replayed ? Ok(body) : StatusCode(StatusCodes.Status201Created, body);
    }

    private async Task<Account> FindOwnedAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return await dbContext.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Id == id && account.OwnerId == userId, cancellationToken) ?? throw new AccountNotFoundException();
    }
    #endregion
}
