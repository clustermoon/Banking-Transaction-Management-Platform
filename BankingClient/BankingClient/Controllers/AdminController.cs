using BankingClient.Api;
using BankingClient.Authentication;
using BankingClient.Models;
using BankingClient.Models.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

[Authorize(Roles = ClientRoles.Admin)]
[Route("admin")]
public sealed class AdminController(IBankingApiClient api) : Controller
{
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts(CancellationToken cancellationToken) => View(new AdminAccountsViewModel(await api.GetAdminAccountsAsync(cancellationToken)));

    [HttpGet("accounts/{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, CancellationToken cancellationToken)
    {
        var model = new ManageAccountViewModel();
        return await LoadAsync(id, model, cancellationToken) ? View(model) : NotFound();
    }

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
