using System.ComponentModel.DataAnnotations;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BankingClient.Models.BankAccounts;

/// <summary>
/// The transfer form. The source account and the destination choices come from the server,
/// never from the posted form; only the customer's own inputs are bound.
/// </summary>
public sealed class TransferViewModel
{
    [BindNever]
    [ValidateNever]
    public AccountResponse Account { get; set; } = null!;

    /// <summary>The customer's other active accounts, for "Between my accounts".</summary>
    [BindNever]
    [ValidateNever]
    public IReadOnlyList<AccountResponse> DestinationOptions { get; set; } = [];

    public TransferMode Mode { get; set; }

    [Required(ErrorMessage = "Choose or enter the account to transfer to.")]
    public string? ToAccountNumber { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true, ErrorMessage = "Enter an amount between $0.01 and $1,000,000.")]
    public decimal? Amount { get; set; }

    [StringLength(30)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    public Guid IdempotencyKey { get; set; }
}
