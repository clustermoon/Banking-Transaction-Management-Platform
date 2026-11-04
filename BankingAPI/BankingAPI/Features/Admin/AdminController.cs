using System.Linq.Expressions;

using BankingAPI.Data;
using BankingAPI.Domain.Accounts;
using BankingAPI.Domain.Audit;
using BankingAPI.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Features.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.Admin)]
public sealed class AdminController(
    BankingDbContext dbContext,
    AccountAdministrationService administration) : ControllerBase
{
    private const int MaxAuditEntries = 100;

    [HttpGet("accounts")]
    [ProducesResponseType<IReadOnlyList<AdminAccountResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AdminAccountResponse>> ListAccounts(CancellationToken cancellationToken) => await Accounts(_ => true).ToListAsync(cancellationToken);

    [HttpGet("accounts/{id:guid}")]
    [ProducesResponseType<AdminAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<AdminAccountResponse> GetAccount(Guid id, CancellationToken cancellationToken) => await Accounts(account => account.Id == id).SingleOrDefaultAsync(cancellationToken) ?? throw new AccountNotFoundException();

    [HttpPost("accounts/{id:guid}/freeze")]
    [ProducesResponseType<AuditEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AuditEntryResponse> Freeze(Guid id, StatusChangeRequest request, CancellationToken cancellationToken) => ChangeStatusAsync(administration.FreezeAsync, id, request, cancellationToken);

    [HttpPost("accounts/{id:guid}/unfreeze")]
    [ProducesResponseType<AuditEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AuditEntryResponse> Unfreeze(Guid id, StatusChangeRequest request, CancellationToken cancellationToken) => ChangeStatusAsync(administration.UnfreezeAsync, id, request, cancellationToken);

    [HttpPost("accounts/{id:guid}/close")]
    [ProducesResponseType<AuditEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AuditEntryResponse> Close(Guid id, StatusChangeRequest request, CancellationToken cancellationToken) => ChangeStatusAsync(administration.CloseAsync, id, request, cancellationToken);

    [HttpGet("audit")]
    [ProducesResponseType<IReadOnlyList<AuditEntryResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AuditEntryResponse>> Audit([FromQuery] AuditQuery query, CancellationToken cancellationToken) => await AuditLog(entry => query.AccountId == null || entry.AccountId == query.AccountId).Take(MaxAuditEntries).ToListAsync(cancellationToken);

    private async Task<AuditEntryResponse> ChangeStatusAsync(Func<Guid, Guid, string, CancellationToken, Task<AuditEntry>> change, Guid accountId, StatusChangeRequest request, CancellationToken cancellationToken)
    {
        var entry = await change(User.GetUserId(), accountId, request.Reason, cancellationToken);
        return await AuditLog(candidate => candidate.Id == entry.Id).SingleAsync(cancellationToken);
    }

    private IQueryable<AdminAccountResponse> Accounts(Expression<Func<Account, bool>> filter) =>
        from account in dbContext.Accounts.AsNoTracking().Where(filter)
        join owner in dbContext.Users on account.OwnerId equals owner.Id
        orderby owner.DisplayName, account.OpenedAt
        select new AdminAccountResponse(account.Id, account.AccountNumber, account.Name, account.Type, account.Status, account.Balance, account.OpenedAt, owner.DisplayName, owner.Email!);

    private IQueryable<AuditEntryResponse> AuditLog(Expression<Func<AuditEntry, bool>> filter) =>
        from entry in dbContext.AuditEntries.AsNoTracking().Where(filter)
        join account in dbContext.Accounts on entry.AccountId equals account.Id
        join owner in dbContext.Users on account.OwnerId equals owner.Id
        join actor in dbContext.Users on entry.ActorUserId equals actor.Id
        orderby entry.Id descending
        select new AuditEntryResponse(entry.Id, entry.OccurredAt, actor.DisplayName, account.Id, account.AccountNumber, account.Name, owner.DisplayName, entry.FromStatus, entry.ToStatus, entry.Reason);
}
