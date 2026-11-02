using System.Globalization;

using BankingAPI.Data;

using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Domain.Ledger;

/// <summary>
/// Verifies that balances and the ledger agree. For each account: every entry's BalanceAfter
/// equals the running total of its history up to that entry, and the account's balance equals
/// the total of all its entries. Non-negative balances are enforced by the database itself.
/// </summary>
public static class LedgerInvariants
{
    public static async Task<IReadOnlyList<string>> FindViolationsAsync(
        BankingDbContext dbContext, IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts.AsNoTracking().Where(account => accountIds.Contains(account.Id)).Select(account => new { account.Id, account.Balance }).ToListAsync(cancellationToken);

        var entriesByAccount = (await dbContext.LedgerEntries.AsNoTracking().Where(entry => accountIds.Contains(entry.AccountId)).OrderBy(entry => entry.Id).ToListAsync(cancellationToken)).ToLookup(entry => entry.AccountId);

        var violations = new List<string>();
        foreach (var account in accounts)
        {
            var runningTotal = 0m;
            foreach (var entry in entriesByAccount[account.Id])
            {
                runningTotal += entry.SignedAmount;
                if (entry.BalanceAfter != runningTotal)
                {
                    violations.Add(string.Create(CultureInfo.InvariantCulture, $"Entry {entry.Id} on account {account.Id} records a balance of {entry.BalanceAfter}, but its history totals {runningTotal}."));
                }
            }

            if (account.Balance != runningTotal)
            {
                violations.Add(string.Create(CultureInfo.InvariantCulture, $"Account {account.Id} has a balance of {account.Balance}, but its ledger totals {runningTotal}."));
            }
        }

        return violations;
    }
}
