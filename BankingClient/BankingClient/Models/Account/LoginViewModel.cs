using System.ComponentModel.DataAnnotations;

namespace BankingClient.Models.Account;

/// <summary>
/// Provides data for the login view, including the user's email, password, and optional return URL.
/// </summary>
public sealed class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public bool SessionExpired { get; init; }

}
