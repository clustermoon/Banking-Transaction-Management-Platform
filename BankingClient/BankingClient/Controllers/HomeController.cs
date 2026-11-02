using System.Diagnostics;

using BankingClient.Models;

using Microsoft.AspNetCore.Mvc;
namespace BankingClient.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    // Shown for any error status without its own page, notably 404. Another customer's
    // account renders this exact page, so it's indistinguishable from a missing one.
    [HttpGet("error/{statusCode:int}")]
    public IActionResult Status(int statusCode) => View(statusCode);

}
