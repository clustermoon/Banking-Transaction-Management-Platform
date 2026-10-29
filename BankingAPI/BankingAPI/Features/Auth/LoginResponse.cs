namespace BankingAPI.Features.Auth;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
