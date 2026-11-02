using System.ComponentModel.DataAnnotations;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BankingClient.Models.BankAccounts;

/// <summary>
/// A deposit or withdrawal form. Account and Kind come from the server, never from the posted form.
/// </summary>
public sealed class MoveMoneyViewModel
{
    [BindNever]
    [ValidateNever]
    public AccountResponse Account { get; set; } = null!;

    [BindNever]
    public LedgerEntryType Kind { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true, ErrorMessage = "Enter an amount between $0.01 and $1,000,000.")]
    public decimal? Amount { get; set; }

    [StringLength(100)]
    public string? Description { get; set; }

    public Guid IdempotencyKey { get; set; }
}
