namespace BankingClient.Api;

/// <summary>
/// An expected API failure: a problem title, plus per-field errors when the API rejected input.
/// </summary>
public sealed record ApiProblem(string Title, IReadOnlyDictionary<string, string[]> Errors);
