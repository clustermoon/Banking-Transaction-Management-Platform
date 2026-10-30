namespace BankingAPI.Features.Auth;

/// <summary>
/// Data required to create a new customer account.
/// </summary>
/// <param name="Email">The customer's email address and sign-in identifier.</param>
/// <param name="Password">The password used to authenticate the customer.</param>
/// <param name="DisplayName">The name displayed for the customer.</param>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
