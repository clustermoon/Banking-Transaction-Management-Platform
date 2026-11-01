namespace BankingAPI.Features.Accounts;

/// <summary>
/// An inclusive range of UTC dates. Both are optional; the default is the last 30 days.
/// </summary>
public sealed record HistoryQuery(DateOnly? From, DateOnly? To);
