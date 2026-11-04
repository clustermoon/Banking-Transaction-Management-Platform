using BankingClient.Api;
using BankingClient.Authentication;
using BankingClient.Models.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

/// <summary>
/// Displays the dashboard view, including a list of accounts and summary statistics. This controller handles requests to view the dashboard page and retrieves the necessary data from the banking API.
/// </summary>
/// <param name="api"></param>
[Authorize(Roles = ClientRoles.Customer)]
[Route("dashboard")]
public sealed class DashboardController(IBankingApiClient api) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(new DashboardViewModel(await api.GetAccountsAsync(cancellationToken)));
}
