using BankingClient.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

/// <summary>
/// Displays the user's profile information, including their name, email, and account details. This controller handles requests to view the profile page and retrieves the necessary data from the banking API.
/// </summary>
/// <param name="api"></param>
[Authorize]
[Route("profile")]
public sealed class ProfileController(IBankingApiClient api) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await api.GetProfileAsync(accessToken: null, cancellationToken));
}
