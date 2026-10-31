namespace BankingClient.Api.Contracts;

/// <summary>
/// Data structure for a Login Request matching the same schema from the API (back-end).
/// </summary>
/// <param name="Email"></param>
/// <param name="Password"></param>
public sealed record LoginRequest(string Email, string Password);
