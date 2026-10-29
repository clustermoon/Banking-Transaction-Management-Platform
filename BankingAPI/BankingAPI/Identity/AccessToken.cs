namespace BankingAPI.Identity;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
