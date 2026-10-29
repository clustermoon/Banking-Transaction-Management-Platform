namespace BankingAPI.Features.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
