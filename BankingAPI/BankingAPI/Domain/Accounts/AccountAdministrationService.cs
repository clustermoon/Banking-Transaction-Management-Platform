using BankingAPI.Data;
using BankingAPI.Domain.Audit;

using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Domain.Accounts;

public sealed class AccountAdministrationService(BankingDbContext dbContext, TimeProvider timeProvider)
{
    public const int MaxReasonLength = 200;

    public Task<AuditEntry> FreezeAsync(Guid actorUserId, Guid accountId, string reason, CancellationToken cancellationToken) => ChangeStatusAsync(actorUserId, accountId, reason, account => account.Freeze(), cancellationToken);

    public Task<AuditEntry> UnfreezeAsync(Guid actorUserId, Guid accountId, string reason, CancellationToken cancellationToken) => ChangeStatusAsync(actorUserId, accountId, reason, account => account.Unfreeze(), cancellationToken);

    public Task<AuditEntry> CloseAsync(Guid actorUserId, Guid accountId, string reason, CancellationToken cancellationToken) => ChangeStatusAsync(actorUserId, accountId, reason, account => account.Close(), cancellationToken);

    private async Task<AuditEntry> ChangeStatusAsync(Guid actorUserId, Guid accountId, string reason, Action<Account> change, CancellationToken cancellationToken)
    {
        var trimmedReason = reason.Trim();
        if (trimmedReason.Length == 0)
        {
            throw new BusinessRuleException("A reason is required for every account change.");
        }

        if (trimmedReason.Length > MaxReasonLength)
        {
            throw new BusinessRuleException($"A reason can be at most {MaxReasonLength} characters.");
        }

        var account = await dbContext.Accounts.SingleOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken) ?? throw new AccountNotFoundException();

        var fromStatus = account.Status;
        change(account);

        var entry = AuditEntry.Record(actorUserId, account.Id, fromStatus, account.Status, trimmedReason, timeProvider.GetUtcNow());
        dbContext.AuditEntries.Add(entry);

        await dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }
}
