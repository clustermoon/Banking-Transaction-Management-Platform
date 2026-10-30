namespace BankingClient.Api.Contracts;

/// <summary>
/// Data structure for a Profile Response matching the same schema from the API (back-end).
/// </summary>
/// <param name="Id"></param>
/// <param name="Email"></param>
/// <param name="DisplayName"></param>
/// <param name="Roles"></param>
public sealed record ProfileResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
