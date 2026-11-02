using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using BankingAPI.Data;
using BankingAPI.Domain.Accounts;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Domain.Ledger;

/// <summary>
/// The only code that moves money. Each operation changes the balance with one conditional
/// UPDATE that takes the account's row lock, records a ledger entry from that update's output,
/// and is idempotent: a retried request returns its original result instead of moving money again.
/// </summary>
public sealed class LedgerService(BankingDbContext dbContext, TimeProvider timeProvider)
{
    public Task<LedgerOperationResult> DepositAsync(LedgerRequest request, CancellationToken cancellationToken) => ExecuteAsync(LedgerEntryType.Deposit, request, cancellationToken);

    public Task<LedgerOperationResult> WithdrawAsync(LedgerRequest request, CancellationToken cancellationToken) => ExecuteAsync(LedgerEntryType.Withdrawal, request, cancellationToken);

    private async Task<LedgerOperationResult> ExecuteAsync(LedgerEntryType type, LedgerRequest request, CancellationToken cancellationToken)
    {
        EnsureValidAmount(request.Amount);
        var requestHash = HashRequest(type, request);

        // A retry of a request that already completed returns the original result.
        var existing = await FindRecordAsync(request, cancellationToken);
        if (existing is not null)
        {
            return await ReplayAsync(existing, requestHash, cancellationToken);
        }

        var occurredAt = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var balanceAfter = await ApplyToBalanceAsync(type, request, cancellationToken);
        if (balanceAfter is null)
        {
            throw await ExplainRefusalAsync(type, request, cancellationToken);
        }

        var entry = LedgerEntry.Record(request.AccountId, type, request.Amount, balanceAfter.Value, request.Description, occurredAt);
        dbContext.LedgerEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.IdempotencyRecords.Add(IdempotencyRecord.Create(request.UserId, request.IdempotencyKey, requestHash, entry.Id, occurredAt));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            // An identical request committed first. Undo this attempt's balance change and
            // entry, and return the winner's result, so money moves exactly once.
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var winner = await FindRecordAsync(request, cancellationToken);
            return await ReplayAsync(winner!, requestHash, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new LedgerOperationResult(entry, Replayed: false);
    }

    /// <summary>
    /// One statement checks every rule and changes the balance while holding the row lock, so
    /// concurrent operations on an account are applied one at a time against the latest balance.
    /// Returns the new balance, or null if the rules refused the change.
    /// </summary>
    private async Task<decimal?> ApplyToBalanceAsync(LedgerEntryType type, LedgerRequest request, CancellationToken cancellationToken)
    {
        var active = nameof(AccountStatus.Active);

        var balances = type switch
        {
            LedgerEntryType.Deposit => await dbContext.Database.SqlQuery<decimal>($"""
                UPDATE Accounts
                SET Balance = Balance + {request.Amount}
                OUTPUT inserted.Balance AS Value
                WHERE Id = {request.AccountId} AND OwnerId = {request.UserId} AND Status = {active}
                """).ToListAsync(cancellationToken),

            LedgerEntryType.Withdrawal => await dbContext.Database.SqlQuery<decimal>($"""
                UPDATE Accounts
                SET Balance = Balance - {request.Amount}
                OUTPUT inserted.Balance AS Value
                WHERE Id = {request.AccountId} AND OwnerId = {request.UserId} AND Status = {active}
                  AND Balance >= {request.Amount}
                """).ToListAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported ledger entry type."),
        };

        return balances.Count == 1 ? balances[0] : null;
    }

    // The update changes nothing for four reasons; work out which, for an accurate message.
    // A missing account and someone else's account both read as "not found".
    private async Task<Exception> ExplainRefusalAsync(LedgerEntryType type, LedgerRequest request, CancellationToken cancellationToken)
    {
        var status = await dbContext.Accounts.AsNoTracking()
            .Where(account => account.Id == request.AccountId && account.OwnerId == request.UserId)
            .Select(account => (AccountStatus?)account.Status)
            .SingleOrDefaultAsync(cancellationToken);

        return status switch
        {
            null => new AccountNotFoundException(),
            AccountStatus.Frozen => new BusinessRuleException("This account is frozen, so money can't move in or out of it."),
            AccountStatus.Closed => new BusinessRuleException("This account is closed."),
            _ when type == LedgerEntryType.Withdrawal => new BusinessRuleException("Insufficient funds for this withdrawal."),
            _ => new InvalidOperationException("The balance update was refused for an unexpected reason."),
        };
    }

    private Task<IdempotencyRecord?> FindRecordAsync(LedgerRequest request, CancellationToken cancellationToken) => dbContext.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(
        record => record.UserId == request.UserId && record.Key == request.IdempotencyKey, cancellationToken);

    private async Task<LedgerOperationResult> ReplayAsync(IdempotencyRecord record, string requestHash, CancellationToken cancellationToken)
    {
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new BusinessRuleException("This request key was already used for a different request.");
        }

        var entry = await dbContext.LedgerEntries.AsNoTracking().SingleAsync(candidate => candidate.Id == record.LedgerEntryId, cancellationToken);
        return new LedgerOperationResult(entry, Replayed: true);
    }

    // decimal(18,2) would silently round extra decimal places, changing the amount.
    private static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0m || decimal.Round(amount, 2) != amount)
        {
            throw new BusinessRuleException("Amounts must be greater than zero, with at most two decimal places.");
        }
    }

    // A fixed text form of the request, so a retry hashes identically and any change doesn't.
    private static string HashRequest(LedgerEntryType type, LedgerRequest request)
    {
        var canonical = string.Create(CultureInfo.InvariantCulture, $"{type}|{request.AccountId}|{request.Amount:0.00}|{request.Description}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    // 2627 is a primary key or unique constraint violation; 2601 is a unique index violation.
    private static bool IsDuplicateKey(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2627 or 2601 };
}
