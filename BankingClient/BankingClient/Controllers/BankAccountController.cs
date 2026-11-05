using BankingClient.Api;
using BankingClient.Api.Contracts;
using BankingClient.Authentication;
using BankingClient.Models;
using BankingClient.Models.BankAccounts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

/// <summary>
/// Displays the bank account views, including account details, transaction history, and forms for deposits, withdrawals, and transfers. This controller handles requests to view and manage bank accounts and retrieves the necessary data from the banking API.
/// </summary>
/// <param name="api"></param>
[Authorize(Roles = ClientRoles.Customer)]
[Route("accounts")]
public sealed class BankAccountsController(IBankingApiClient api) : Controller
{
    #region GET Details
    /// <summary>
    /// Gets the details of a specific bank account, including the account information and transaction history for the specified date range. If the account is not found, returns a 404 result; otherwise, returns the view with the account details and history.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Opens the new account view, providing a form for the user to select the account type and enter the account name. This action returns the view with an empty model for the user to fill out.
    /// </summary>
    /// <returns></returns>
    [HttpGet("open")]
    public IActionResult Open() => View(new OpenAccountViewModel());
    #endregion

    #region POST Open account
    /// <summary>
    /// Opens a new bank account, validating the input and calling the banking API. If the model state is invalid or the API call fails, returns the view with validation errors; otherwise, redirects to the account details page with a success message.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// <summary>
    /// Deposits money into the specified account, preparing the deposit view model with the account information. If the account is not found, returns a 404 result; otherwise, returns the view with the prepared model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/deposit")]
    public Task<IActionResult> Deposit(Guid id, CancellationToken cancellationToken) => ShowMoveMoneyAsync(id, LedgerEntryType.Deposit, cancellationToken);

    /// <summary>
    /// Deposits money into the specified account, validating the input and calling the banking API. If the account is not found, returns a 404 result; if the model state is invalid or the API call fails, returns the view with validation errors; otherwise, redirects to the account details page with a success message.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/deposit")]
    public Task<IActionResult> Deposit(Guid id, MoveMoneyViewModel model, CancellationToken cancellationToken) => MoveMoneyAsync(id, LedgerEntryType.Deposit, model, cancellationToken);
    #endregion

    #region GET/POST Withdraw
    /// <summary>
    /// Withdraws money from the specified account, preparing the withdrawal view model with the account information. If the account is not found, returns a 404 result; otherwise, returns the view with the prepared model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/withdraw")]
    public Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken) => ShowMoveMoneyAsync(id, LedgerEntryType.Withdrawal, cancellationToken);

    /// <summary>
    /// Withdraws money from the specified account, validating the input and calling the banking API. If the account is not found, returns a 404 result; if the model state is invalid or the API call fails, returns the view with validation errors; otherwise, redirects to the account details page with a success message.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/withdraw")]
    public Task<IActionResult> Withdraw(Guid id, MoveMoneyViewModel model, CancellationToken cancellationToken) => MoveMoneyAsync(id, LedgerEntryType.Withdrawal, model, cancellationToken);
    #endregion

    #region GET Transfer
    /// <summary>
    /// Transfers money from the source account to the specified destination account, preparing the transfer view model with the source account and destination options. If the source account is not found or is inactive, returns a redirect result; otherwise, returns the view with the prepared model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="mode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid id, TransferMode mode, CancellationToken cancellationToken)
    {
        // One key per page load, as for deposits: a double-click or resubmission moves money once.
        var model = new TransferViewModel { Mode = mode, IdempotencyKey = Guid.NewGuid() };
        return await PrepareTransferAsync(id, model, cancellationToken) ?? View(model);
    }
    #endregion

    #region POST Transfer
    /// <summary>
    /// Transfers money from the source account to the specified destination account, validating the input and calling the banking API. If the source account is not found or is inactive, returns a redirect result; if the model state is invalid or the API call fails, returns the view with validation errors; otherwise, redirects to the account details page with a success message.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid id, TransferViewModel model, CancellationToken cancellationToken)
    {
        if (await PrepareTransferAsync(id, model, cancellationToken) is { } redirect)
        {
            return redirect;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Accept numbers copied with spaces or dashes; the API expects digits only.
        var accountNumber = new string([.. model.ToAccountNumber!.Where(char.IsAsciiDigit)]);
        var result = await api.TransferAsync(
            id, new TransferMoneyRequest(accountNumber, model.Amount!.Value, model.Note), model.IdempotencyKey, cancellationToken);

        if (result.Value is not { } movement)
        {
            ModelState.AddApiProblem(result.Problem!);
            return View(model);
        }

        // Name the destination only if it's one of the customer's own accounts.
        var destination = model.DestinationOptions.FirstOrDefault(account => account.AccountNumber == accountNumber)?.Name
            ?? $"•••• {accountNumber[^4..]}";
        TempData[TempDataKeys.Notice] = movement.Replayed
            ? "That request was already completed, so it wasn't repeated."
            : $"Transferred {Math.Abs(movement.Entry.SignedAmount):C} to {destination}.";

        return RedirectToAction(nameof(Details), new { id });
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Prepares the transfer view model with the source account and destination options. If the source account is not found or is inactive, returns a redirect result; otherwise, returns null to indicate that the view model is ready for display.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<IActionResult?> PrepareTransferAsync(
        Guid id, TransferViewModel model, CancellationToken cancellationToken)
    {
        var account = await api.GetAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        if (account.Status != AccountStatus.Active)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        model.Account = account;
        model.DestinationOptions =
        [
            .. (await api.GetAccountsAsync(cancellationToken))
                .Where(candidate => candidate.Id != id && candidate.Status == AccountStatus.Active),
        ];
        return null;
    }

    /// <summary>
    /// Shows the move money view for deposits or withdrawals, retrieving the source account and preparing the view model. If the source account is not found, returns a 404 result; otherwise, returns the view with the prepared model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="kind"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<IActionResult> ShowMoveMoneyAsync(Guid id, LedgerEntryType kind, CancellationToken cancellationToken)
    {
        var account = await api.GetAccountAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        return View("MoveMoney", new MoveMoneyViewModel { Account = account, Kind = kind, IdempotencyKey = Guid.NewGuid() });
    }

    /// <summary>
    /// Moves money for deposits or withdrawals, validating the input and calling the appropriate API method. If the source account is not found, returns a 404 result; if the model state is invalid or the API call fails, returns the view with validation errors; otherwise, redirects to the account details page with a success message.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="kind"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
