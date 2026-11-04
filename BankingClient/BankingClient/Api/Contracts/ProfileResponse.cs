namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for retrieving a user's profile information from the banking API, including the user's unique identifier, email address, display name, and roles. This record is used to encapsulate the necessary information returned by the API when requesting profile details.
/// </summary>
/// <param name="Id"></param>
/// <param name="Email"></param>
/// <param name="DisplayName"></param>
/// <param name="Roles"></param>
public sealed record ProfileResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
