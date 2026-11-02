using BankingClient.Api;
using BankingClient.Models.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

[Authorize]
[Route("dashboard")]
public sealed class DashboardController(IBankingApiClient api) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(new DashboardViewModel(await api.GetAccountsAsync(cancellationToken)));
}
