namespace BankingClient.Api.Contracts;

/// <param name="SignedAmount">Positive for money in, negative for money out.</param>
public sealed record LedgerEntryResponse(long Id, DateTimeOffset OccurredAt, LedgerEntryType Type, string Description, decimal SignedAmount, decimal BalanceAfter);
