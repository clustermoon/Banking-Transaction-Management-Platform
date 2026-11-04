namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for the response received after a successful login operation, including the access token and its expiration time. This record is used to encapsulate the necessary information returned by the banking API upon user authentication.
/// </summary>
/// <param name="AccessToken"></param>
/// <param name="ExpiresAt"></param>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
