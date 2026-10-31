namespace BankingAPI.Features.Auth;

/// <summary>
/// Details returned after a customer account is successfully created.
/// </summary>
/// <param name="Id">The unique identifier assigned to the customer.</param>
/// <param name="Email">The email address associated with the customer account.</param>
/// <param name="DisplayName">The customer's display name.</param>
public sealed record RegisterResponse(Guid Id, string Email, string DisplayName);
