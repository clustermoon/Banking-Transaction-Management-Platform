using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using BankingAPI.Data;
using BankingAPI.Domain.Accounts;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Domain.Ledger;

public sealed class LedgerService(BankingDbContext dbContext, TimeProvider timeProvider)
{
    public const int MaxTransferNoteLength = 30;

    public Task<LedgerOperationResult> DepositAsync(LedgerRequest request, CancellationToken cancellationToken)
    {
        EnsureValidAmount(request.Amount);
        var requestHash = HashRequest(LedgerEntryType.Deposit, request);

        return RunIdempotentAsync(request.UserId, request.IdempotencyKey, requestHash, async occurredAt =>
        {
            var balanceAfter = await UpdateBalanceAsync(
                $"""
                UPDATE Accounts
                SET Balance = Balance + {request.Amount}
                OUTPUT inserted.Balance AS Value
                WHERE Id = {request.AccountId} AND OwnerId = {request.UserId} AND Status = {Active}
                """,
                cancellationToken);

            if (balanceAfter is null)
            {
                throw await ExplainRefusalAsync(request.AccountId, request.UserId, insufficientFundsMessage: null, cancellationToken);
            }

            return Add(LedgerEntry.Record(request.AccountId, LedgerEntryType.Deposit, request.Amount, balanceAfter.Value, request.Description, occurredAt));
        }, cancellationToken);
    }

    public Task<LedgerOperationResult> WithdrawAsync(LedgerRequest request, CancellationToken cancellationToken)
    {
        EnsureValidAmount(request.Amount);
        var requestHash = HashRequest(LedgerEntryType.Withdrawal, request);

        return RunIdempotentAsync(request.UserId, request.IdempotencyKey, requestHash, async occurredAt =>
        {
            var balanceAfter = await DebitOwnedAccountAsync(request.AccountId, request.UserId, request.Amount, cancellationToken);
            if (balanceAfter is null)
            {
                throw await ExplainRefusalAsync(request.AccountId, request.UserId, "Insufficient funds for this withdrawal.", cancellationToken);
            }

            return Add(LedgerEntry.Record(request.AccountId, LedgerEntryType.Withdrawal, request.Amount, balanceAfter.Value, request.Description, occurredAt));
        }, cancellationToken);
    }

    public Task<LedgerOperationResult> TransferAsync(TransferRequest request, CancellationToken cancellationToken)
    {
        EnsureValidAmount(request.Amount);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note?.Length > MaxTransferNoteLength)
        {
            throw new BusinessRuleException($"A transfer note can be at most {MaxTransferNoteLength} characters.");
        }

        var requestHash = HashRequest(string.Create(CultureInfo.InvariantCulture, $"Transfer|{request.FromAccountId}|{request.ToAccountNumber}|{request.Amount:0.00}|{note}"));

        return RunIdempotentAsync(request.UserId, request.IdempotencyKey, requestHash, async occurredAt =>
        {
            var source = await dbContext.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Id == request.FromAccountId && account.OwnerId == request.UserId, cancellationToken) ?? throw new AccountNotFoundException();

            var destination = await dbContext.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.AccountNumber == request.ToAccountNumber, cancellationToken);

            if (destination is null || destination.Status != AccountStatus.Active)
            {
                throw new BusinessRuleException("We couldn't find an active account with that number.");
            }

            if (destination.Id == source.Id)
            {
                throw new BusinessRuleException("Choose a different account to transfer to.");
            }

            var (sourceBalance, destinationBalance) = await DebitAndCreditInLockOrderAsync(source.Id, destination.Id, request.UserId, request.Amount, cancellationToken);

            var isBetweenOwnAccounts = destination.OwnerId == request.UserId;
            var destinationLabel = isBetweenOwnAccounts ? destination.Name : destination.MaskedAccountNumber;
            var sourceLabel = isBetweenOwnAccounts ? source.Name : source.MaskedAccountNumber;
            var transferId = Guid.NewGuid();

            var outgoing = Add(LedgerEntry.RecordTransferLeg(source.Id, LedgerEntryType.TransferOut, request.Amount, sourceBalance, Describe("Transfer to", destinationLabel, note), occurredAt, transferId, destination.Id));
            Add(LedgerEntry.RecordTransferLeg(destination.Id, LedgerEntryType.TransferIn, request.Amount, destinationBalance, Describe("Transfer from", sourceLabel, note), occurredAt, transferId, source.Id));

            return outgoing;
        }, cancellationToken);
    }

    private async Task<LedgerOperationResult> RunIdempotentAsync(Guid userId, Guid idempotencyKey, string requestHash, Func<DateTimeOffset, Task<LedgerEntry>> apply, CancellationToken cancellationToken)
    {
        var existing = await FindRecordAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await ReplayAsync(existing, requestHash, cancellationToken);
        }

        var occurredAt = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var recorded = await apply(occurredAt);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.IdempotencyRecords.Add(IdempotencyRecord.Create(userId, idempotencyKey, requestHash, recorded.Id, occurredAt));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var winner = await FindRecordAsync(userId, idempotencyKey, cancellationToken);
            return await ReplayAsync(winner!, requestHash, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new LedgerOperationResult(recorded, Replayed: false);
    }

    private async Task<(decimal SourceBalance, decimal DestinationBalance)> DebitAndCreditInLockOrderAsync(Guid sourceId, Guid destinationId, Guid userId, decimal amount, CancellationToken cancellationToken)
    {
        if (sourceId.CompareTo(destinationId) < 0)
        {
            var sourceBalance = await DebitOrExplainAsync(sourceId, userId, amount, cancellationToken);
            var destinationBalance = await CreditActiveAccountOrRefuseAsync(destinationId, amount, cancellationToken);
            return (sourceBalance, destinationBalance);
        }
        else
        {
            var destinationBalance = await CreditActiveAccountOrRefuseAsync(destinationId, amount, cancellationToken);
            var sourceBalance = await DebitOrExplainAsync(sourceId, userId, amount, cancellationToken);
            return (sourceBalance, destinationBalance);
        }
    }

    private async Task<decimal> DebitOrExplainAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken) => await DebitOwnedAccountAsync(accountId, userId, amount, cancellationToken) ?? throw await ExplainRefusalAsync(accountId, userId, "Insufficient funds for this transfer.", cancellationToken);

    private async Task<decimal> CreditActiveAccountOrRefuseAsync(
        Guid accountId, decimal amount, CancellationToken cancellationToken) => await UpdateBalanceAsync(
        $"""
        UPDATE Accounts
        SET Balance = Balance + {amount}
        OUTPUT inserted.Balance AS Value
        WHERE Id = {accountId} AND Status = {Active}
        """, cancellationToken) ?? throw new BusinessRuleException("We couldn't find an active account with that number.");

    private Task<decimal?> DebitOwnedAccountAsync(
        Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken) => UpdateBalanceAsync(
        $"""
        UPDATE Accounts
        SET Balance = Balance - {amount}
        OUTPUT inserted.Balance AS Value
        WHERE Id = {accountId} AND OwnerId = {userId} AND Status = {Active} AND Balance >= {amount}
        """, cancellationToken);

    private async Task<decimal?> UpdateBalanceAsync(FormattableString update, CancellationToken cancellationToken)
    {
        var balances = await dbContext.Database.SqlQuery<decimal>(update).ToListAsync(cancellationToken);
        return balances.Count == 1 ? balances[0] : null;
    }

    private static string Active => nameof(AccountStatus.Active);

    private async Task<Exception> ExplainRefusalAsync(Guid accountId, Guid userId, string? insufficientFundsMessage, CancellationToken cancellationToken)
    {
        var status = await dbContext.Accounts.AsNoTracking().Where(account => account.Id == accountId && account.OwnerId == userId).Select(account => (AccountStatus?)account.Status).SingleOrDefaultAsync(cancellationToken);

        return status switch
        {
            null => new AccountNotFoundException(),
            AccountStatus.Frozen => new BusinessRuleException("This account is frozen, so money can't move in or out of it."),
            AccountStatus.Closed => new BusinessRuleException("This account is closed."),
            _ when insufficientFundsMessage is not null => new BusinessRuleException(insufficientFundsMessage),
            _ => new InvalidOperationException("The balance update was refused for an unexpected reason."),
        };
    }

    private LedgerEntry Add(LedgerEntry entry)
    {
        dbContext.LedgerEntries.Add(entry);
        return entry;
    }

    private Task<IdempotencyRecord?> FindRecordAsync(Guid userId, Guid key, CancellationToken cancellationToken) => dbContext.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(record => record.UserId == userId && record.Key == key, cancellationToken);

    private async Task<LedgerOperationResult> ReplayAsync(IdempotencyRecord record, string requestHash, CancellationToken cancellationToken)
    {
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new BusinessRuleException("This request key was already used for a different request.");
        }

        var entry = await dbContext.LedgerEntries.AsNoTracking().SingleAsync(candidate => candidate.Id == record.LedgerEntryId, cancellationToken);
        return new LedgerOperationResult(entry, Replayed: true);
    }

    private static string Describe(string direction, string accountLabel, string? note) => note is null ? $"{direction} {accountLabel}" : $"{direction} {accountLabel}: {note}";

    private static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0m || decimal.Round(amount, 2) != amount)
        {
            throw new BusinessRuleException("Amounts must be greater than zero, with at most two decimal places.");
        }
    }

    private static string HashRequest(LedgerEntryType type, LedgerRequest request) => HashRequest(string.Create(CultureInfo.InvariantCulture, $"{type}|{request.AccountId}|{request.Amount:0.00}|{request.Description}"));

    private static string HashRequest(string canonical) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

    private static bool IsDuplicateKey(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2627 or 2601 };
}
