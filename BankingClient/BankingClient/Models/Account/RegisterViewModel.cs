using System.ComponentModel.DataAnnotations;

namespace BankingClient.Models.Account;

/// <summary>
/// Provides data for the registration view, including the user's display name, email, and password.
/// </summary>
public sealed class RegisterViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
