namespace BankingAPI.Identity;

/// <summary>
/// Represents a bearer access token and the time at which it expires.
/// </summary>
/// <param name="Value">The encoded bearer token value.</param>
/// <param name="ExpiresAt">The date and time after which the token is no longer valid.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
