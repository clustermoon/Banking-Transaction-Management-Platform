using BankingClient.Api;
using BankingClient.Api.Contracts;
using BankingClient.Models;
using BankingClient.Models.BankAccounts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

/// <summary>
/// A customer's bank accounts. Named apart from AccountController, which handles signing in.
/// An account the API reports as not found renders the 404 page, so another customer's
/// account looks exactly like one that doesn't exist.
/// </summary>
[Authorize]
[Route("accounts")]
public sealed class BankAccountsController(IBankingApiClient api) : Controller
{
    #region GET Details
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var account = await api.GetAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        var history = await api.GetHistoryAsync(id, from, to, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return View(new AccountDetailsViewModel
        {
            Account = account,
            History = history.Value,
            HistoryProblem = history.Problem,
            From = history.Value?.From ?? from ?? today.AddDays(-29),
            To = history.Value?.To ?? to ?? today,
        });
    }
    #endregion

    #region GET Open account
    [HttpGet("open")]
    public IActionResult Open() => View(new OpenAccountViewModel());
    #endregion

    #region POST Open account
    [HttpPost("open")]
    public async Task<IActionResult> Open(OpenAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await api.OpenAccountAsync(new OpenAccountRequest(model.Type!.Value, model.Name), cancellationToken);
        if (result.Value is not { } account)
        {
            ModelState.AddApiProblem(result.Problem!);
            return View(model);
        }

        TempData[TempDataKeys.Notice] = $"Your {account.Name} account is open.";
        return RedirectToAction(nameof(Details), new { id = account.Id });
    }
    #endregion

    #region GET/POST Deposit
    [HttpGet("{id:guid}/deposit")]
    public Task<IActionResult> Deposit(Guid id, CancellationToken cancellationToken) => ShowMoveMoneyAsync(id, LedgerEntryType.Deposit, cancellationToken);

    [HttpPost("{id:guid}/deposit")]
    public Task<IActionResult> Deposit(Guid id, MoveMoneyViewModel model, CancellationToken cancellationToken) => MoveMoneyAsync(id, LedgerEntryType.Deposit, model, cancellationToken);
    #endregion

    #region GET/POST Withdraw
    [HttpGet("{id:guid}/withdraw")]
    public Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken) => ShowMoveMoneyAsync(id, LedgerEntryType.Withdrawal, cancellationToken);

    [HttpPost("{id:guid}/withdraw")]
    public Task<IActionResult> Withdraw(Guid id, MoveMoneyViewModel model, CancellationToken cancellationToken) => MoveMoneyAsync(id, LedgerEntryType.Withdrawal, model, cancellationToken);
    #endregion

    #region Helpers
    private async Task<IActionResult> ShowMoveMoneyAsync(Guid id, LedgerEntryType kind, CancellationToken cancellationToken)
    {
        var account = await api.GetAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        // One key per page load. Every submission of this form, including a double-click or a
        // browser resubmit, carries the same key, so the API moves the money at most once.
        return View("MoveMoney", new MoveMoneyViewModel { Account = account, Kind = kind, IdempotencyKey = Guid.NewGuid() });
    }

    private async Task<IActionResult> MoveMoneyAsync(Guid id, LedgerEntryType kind, MoveMoneyViewModel model, CancellationToken cancellationToken)
    {
        var account = await api.GetAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        model.Account = account;
        model.Kind = kind;
        if (!ModelState.IsValid)
        {
            return View("MoveMoney", model);
        }

        var request = new MoneyMovementRequest(model.Amount!.Value, model.Description);
        var result = kind == LedgerEntryType.Deposit ? await api.DepositAsync(id, request, model.IdempotencyKey, cancellationToken) : await api.WithdrawAsync(id, request, model.IdempotencyKey, cancellationToken);

        // A refused request (insufficient funds, a frozen account) never records the key,
        // so the form keeps it and the customer can correct and resubmit.
        if (result.Value is not { } movement)
        {
            ModelState.AddApiProblem(result.Problem!);
            return View("MoveMoney", model);
        }

        TempData[TempDataKeys.Notice] = movement.Replayed ? "That request was already completed, so it wasn't repeated." : $"{(kind == LedgerEntryType.Deposit ? "Deposited" : "Withdrew")} {Math.Abs(movement.Entry.SignedAmount):C}.";

        return RedirectToAction(nameof(Details), new { id });
    }
    #endregion
}
