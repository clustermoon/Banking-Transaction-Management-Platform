namespace BankingClient.Api.Contracts;

/// <summary>
/// Data structure for a Login Response matching the same schema from the API (back-end).
/// </summary>
/// <param name="AccessToken"></param>
/// <param name="ExpiresAt"></param>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
