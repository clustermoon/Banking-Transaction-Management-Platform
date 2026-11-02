using System.Globalization;

using BankingAPI.Data;

using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Domain.Ledger;

/// <summary>
/// Verifies that balances and the ledger agree. For each account checked: every entry's
/// BalanceAfter equals the running total of its history up to that entry, and the account's
/// balance equals the total of all its entries. For every transfer those accounts take part in:
/// exactly one outgoing and one incoming entry, for the same amount, each naming the other's
/// account. Non-negative balances and entry shapes are enforced by the database itself.
/// </summary>
public static class LedgerInvariants
{
    public static async Task<IReadOnlyList<string>> FindViolationsAsync(BankingDbContext dbContext, IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts.AsNoTracking().Where(account => accountIds.Contains(account.Id)).Select(account => new { account.Id, account.Balance }).ToListAsync(cancellationToken);

        var entries = await dbContext.LedgerEntries.AsNoTracking().Where(entry => accountIds.Contains(entry.AccountId)).OrderBy(entry => entry.Id).ToListAsync(cancellationToken);
        var entriesByAccount = entries.ToLookup(entry => entry.AccountId);

        var violations = new List<string>();

        foreach (var account in accounts)
        {
            var runningTotal = 0m;
            foreach (var entry in entriesByAccount[account.Id])
            {
                runningTotal += entry.SignedAmount;
                if (entry.BalanceAfter != runningTotal)
                {
                    violations.Add(Describe($"Entry {entry.Id} on account {account.Id} records a balance of {entry.BalanceAfter}, but its history totals {runningTotal}."));
                }
            }

            if (account.Balance != runningTotal)
            {
                violations.Add(Describe($"Account {account.Id} has a balance of {account.Balance}, but its ledger totals {runningTotal}."));
            }
        }

        violations.AddRange(await FindUnbalancedTransfersAsync(dbContext, entries, cancellationToken));
        return violations;
    }

    // A transfer's other half may belong to an account outside the checked set, so both
    // halves of every transfer touching these accounts are loaded together.
    private static async Task<IEnumerable<string>> FindUnbalancedTransfersAsync(
        BankingDbContext dbContext, List<LedgerEntry> entries, CancellationToken cancellationToken)
    {
        var transferIds = entries.Where(entry => entry.TransferId is not null).Select(entry => entry.TransferId).Distinct().ToList();

        if (transferIds.Count == 0)
        {
            return [];
        }

        var legs = await dbContext.LedgerEntries.AsNoTracking().Where(entry => transferIds.Contains(entry.TransferId)).ToListAsync(cancellationToken);

        return legs.GroupBy(entry => entry.TransferId!.Value).Where(transfer => !IsBalanced([.. transfer])).Select(transfer => Describe($"Transfer {transfer.Key} doesn't have exactly one matching outgoing and incoming entry."));
    }

    private static bool IsBalanced(List<LedgerEntry> legs)
    {
        if (legs.Count != 2)
        {
            return false;
        }

        var outgoing = legs.SingleOrDefault(leg => leg.Type == LedgerEntryType.TransferOut);
        var incoming = legs.SingleOrDefault(leg => leg.Type == LedgerEntryType.TransferIn);

        return outgoing is not null && incoming is not null && outgoing.Amount == incoming.Amount && outgoing.CounterpartyAccountId == incoming.AccountId && incoming.CounterpartyAccountId == outgoing.AccountId;
    }

    private static string Describe(FormattableString message) => message.ToString(CultureInfo.InvariantCulture);
}
