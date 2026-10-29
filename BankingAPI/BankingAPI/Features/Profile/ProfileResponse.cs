namespace BankingAPI.Features.Profile;

public sealed record ProfileResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
