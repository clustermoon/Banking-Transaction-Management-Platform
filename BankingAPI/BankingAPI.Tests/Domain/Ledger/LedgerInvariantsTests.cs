using BankingAPI.Data;
using BankingAPI.Domain.Ledger;
using BankingAPI.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Domain.Ledger;

public sealed class LedgerInvariantsTests(BankingApiFactory factory)
{
    [Fact]
    public async Task TransferMissingItsIncomingHalfIsReported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sender = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var receiver = await DatabaseTestData.CreateAccountAsync(factory, cancellationToken);
        var transferId = Guid.NewGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // Corrupt the ledger on purpose: $20 leaves the sender with no matching arrival.
        // The sender's own history and balance are kept consistent, so the only thing
        // wrong is the missing half of the transfer.
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO LedgerEntries (AccountId, Type, Amount, BalanceAfter, Description, OccurredAt)
            VALUES ({sender.Id}, 'Deposit', 50.00, 50.00, 'Test deposit', SYSDATETIMEOFFSET());
            INSERT INTO LedgerEntries (AccountId, Type, Amount, BalanceAfter, Description, OccurredAt, TransferId, CounterpartyAccountId)
            VALUES ({sender.Id}, 'TransferOut', 20.00, 30.00, 'Test transfer', SYSDATETIMEOFFSET(), {transferId}, {receiver.Id});
            UPDATE Accounts SET Balance = 30.00 WHERE Id = {sender.Id};
            """, cancellationToken);

        var violations = await LedgerInvariants.FindViolationsAsync(dbContext, [sender.Id], cancellationToken);

        var violation = Assert.Single(violations);
        Assert.Contains(transferId.ToString(), violation, StringComparison.Ordinal);
    }
}
