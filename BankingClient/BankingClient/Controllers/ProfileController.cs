using BankingClient.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Controllers;

[Authorize]
[Route("profile")]
public sealed class ProfileController(IBankingApiClient api) : Controller
{
    // Loaded from the API rather than the cookie, so a session revoked elsewhere is detected here.
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await api.GetProfileAsync(accessToken: null, cancellationToken));
}
