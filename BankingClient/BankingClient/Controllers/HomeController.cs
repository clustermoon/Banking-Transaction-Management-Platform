using System.Diagnostics;

using BankingClient.Authentication;
using BankingClient.Models;

using Microsoft.AspNetCore.Mvc;
namespace BankingClient.Controllers;

/// <summary>
/// Displays the home page and handles error responses. This controller provides actions for rendering the main landing page, displaying error information, and handling specific HTTP status codes.
/// </summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => User.Identity?.IsAuthenticated == true ? SignedInHome.For(User.IsInRole(ClientRoles.Admin)) : View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [HttpGet("error/{statusCode:int}")]
    public IActionResult Status(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View(statusCode);
    }

}
