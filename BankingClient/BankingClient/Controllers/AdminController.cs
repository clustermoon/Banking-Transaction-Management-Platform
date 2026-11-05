using BankingClient.Api;
using BankingClient.Authentication;
using BankingClient.Models;
using BankingClient.Models.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

/// <summary>
/// Displays the admin view, including a list of accounts and the ability to manage account statuses. This controller handles requests to view the admin dashboard, manage individual accounts, and view the audit log, retrieving the necessary data from the banking API.
/// </summary>
/// <param name="api"></param>
[Authorize(Roles = ClientRoles.Admin)]
[Route("admin")]
public sealed class AdminController(IBankingApiClient api) : Controller
{
    /// <summary>
    /// Admin dashboard view, providing an overview of all accounts and their statuses. This action retrieves the list of accounts from the banking API and returns the view with the prepared model.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts(CancellationToken cancellationToken) => View(new AdminAccountsViewModel(await api.GetAdminAccountsAsync(cancellationToken)));

    /// <summary>
    /// Admin manage account view, allowing the admin to change the status of a specific account. This action retrieves the account details and audit history from the banking API and returns the view with the prepared model. If the account is not found, returns a 404 result.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("accounts/{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, CancellationToken cancellationToken)
    {
        var model = new ManageAccountViewModel();
        return await LoadAsync(id, model, cancellationToken) ? View(model) : NotFound();
    }

    /// <summary>
    /// Manages the status of a specific account, allowing the admin to change the account's status and record the reason for the change. This action validates the input, calls the banking API to change the account status, and handles any validation errors or API problems. If successful, it redirects back to the manage account view with a success message; if not, it returns the view with validation errors.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("accounts/{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, ManageAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, model, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await api.ChangeAccountStatusAsync(id, model.ToStatus!.Value, model.Reason, cancellationToken);
        if (result.Value is not { } entry)
        {
            ModelState.AddApiProblem(result.Problem!);
            ModelState.Remove(nameof(ManageAccountViewModel.ToStatus));
            model.ToStatus = null;
            await LoadAsync(id, model, cancellationToken);
            return View(model);
        }

        TempData[TempDataKeys.Notice] = $"{entry.AccountName} changed from {entry.FromStatus} to {entry.ToStatus}.";
        return RedirectToAction(nameof(Manage), new { id });
    }

    /// <summary>
    /// Audit log view, displaying a list of all account status changes and their associated details. This action retrieves the audit log from the banking API and returns the view with the prepared model.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("audit")]
    public async Task<IActionResult> Audit(CancellationToken cancellationToken) => View(await api.GetAuditLogAsync(accountId: null, cancellationToken));

    private async Task<bool> LoadAsync(Guid id, ManageAccountViewModel model, CancellationToken cancellationToken)
    {
        var account = await api.GetAdminAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return false;
        }

        model.Account = account;
        model.History = await api.GetAuditLogAsync(id, cancellationToken);
        return true;
    }
}
