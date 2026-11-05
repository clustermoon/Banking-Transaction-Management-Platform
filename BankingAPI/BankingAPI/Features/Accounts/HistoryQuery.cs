namespace BankingAPI.Features.Accounts;

public sealed record HistoryQuery(DateOnly? From, DateOnly? To);
