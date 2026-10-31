namespace BankingAPI.Features.Profile;

/// <summary>
/// Public profile information for the currently authenticated customer.
/// </summary>
/// <param name="Id">The customer's unique identifier.</param>
/// <param name="Email">The email address associated with the customer account.</param>
/// <param name="DisplayName">The customer's display name.</param>
/// <param name="Roles">The roles assigned to the customer.</param>
public sealed record ProfileResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
