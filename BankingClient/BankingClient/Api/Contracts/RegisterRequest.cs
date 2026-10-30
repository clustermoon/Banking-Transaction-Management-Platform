namespace BankingClient.Api.Contracts;

/// <summary>
/// Data structure for a Register Request matching the same schema from the API (back-end).
/// </summary>
/// <param name="Email"></param>
/// <param name="Password"></param>
/// <param name="DisplayName"></param>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
