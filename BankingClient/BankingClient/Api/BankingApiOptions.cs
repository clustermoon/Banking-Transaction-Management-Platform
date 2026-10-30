using System.ComponentModel.DataAnnotations;

namespace BankingClient.Api;

/// <summary>
/// Where the API lives. Validated at startup, so a missing address fails immediately rather
/// than on the first sign-in.
/// </summary>
public sealed class BankingApiOptions
{
    public const string SectionName = "BankingApi";

    [Required]
    public Uri? BaseUrl { get; init; }
}
