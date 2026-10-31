namespace BankingAPI.Features.Auth;

/// <summary>
/// Access token details returned after successful authentication.
/// </summary>
/// <param name="AccessToken">The bearer token used to access protected endpoints.</param>
/// <param name="ExpiresAt">The date and time when the access token expires.</param>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
