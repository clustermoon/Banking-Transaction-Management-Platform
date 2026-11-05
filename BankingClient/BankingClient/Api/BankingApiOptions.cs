using System.ComponentModel.DataAnnotations;

namespace BankingClient.Api;

/// <summary>
/// A class that represents the configuration options for the banking API client, including the base URL of the API. This class is used to bind configuration settings from appsettings.json or other configuration sources, allowing the application to easily configure and access the banking API.
/// </summary>
public sealed class BankingApiOptions
{
    public const string SectionName = "BankingApi";

    [Required]
    public Uri? BaseUrl { get; init; }
}
