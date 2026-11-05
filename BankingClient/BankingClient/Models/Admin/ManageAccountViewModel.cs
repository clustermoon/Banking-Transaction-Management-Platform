using System.ComponentModel.DataAnnotations;

using BankingClient.Api.Contracts;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BankingClient.Models.Admin;

/// <summary>
/// Provides data for the manage account view, including the account information, audit history, and status change details.
/// </summary>
public sealed class ManageAccountViewModel
{
    [BindNever]
    [ValidateNever]
    public AdminAccountResponse Account { get; set; } = null!;

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<AuditEntryResponse> History { get; set; } = [];

    [Required(ErrorMessage = "Choose a change to make.")]
    public AccountStatus? ToStatus { get; set; }

    [Required(ErrorMessage = "Enter a reason. It's recorded permanently in the audit log.")]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    public IReadOnlyList<AccountStatus> AvailableChanges() => (Account.Status, Account.Balance) switch
    {
        (AccountStatus.Active, 0m) => [AccountStatus.Frozen, AccountStatus.Closed],
        (AccountStatus.Active, _) => [AccountStatus.Frozen],
        (AccountStatus.Frozen, 0m) => [AccountStatus.Active, AccountStatus.Closed],
        (AccountStatus.Frozen, _) => [AccountStatus.Active],
        _ => [],
    };
}
