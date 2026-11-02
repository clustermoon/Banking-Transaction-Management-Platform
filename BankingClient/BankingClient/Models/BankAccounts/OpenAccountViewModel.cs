using System.ComponentModel.DataAnnotations;

using BankingClient.Api.Contracts;

namespace BankingClient.Models.BankAccounts;

public sealed class OpenAccountViewModel
{
    [Required(ErrorMessage = "Choose an account type.")]
    [Display(Name = "Account type")]
    public AccountType? Type { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Account name")]
    public string Name { get; set; } = string.Empty;
}
