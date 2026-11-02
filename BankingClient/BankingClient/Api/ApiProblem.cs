namespace BankingClient.Api;

/// <summary>
/// An expected API failure. Detail carries a business rule's message ("Insufficient funds…");
/// Errors carries per-field validation messages.
/// </summary>
public sealed record ApiProblem(string Title, string? Detail, IReadOnlyDictionary<string, string[]> Errors);
