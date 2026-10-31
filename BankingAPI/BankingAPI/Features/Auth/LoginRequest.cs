namespace BankingAPI.Features.Auth;

/// <summary>
/// Credentials used to authenticate an existing customer.
/// </summary>
/// <param name="Email">The email address associated with the customer account.</param>
/// <param name="Password">The customer's password.</param>
public sealed record LoginRequest(string Email, string Password);
