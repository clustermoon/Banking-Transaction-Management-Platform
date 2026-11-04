using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Authentication;

public static class SignedInHome
{
    public static RedirectToActionResult For(bool isAdmin) => isAdmin ? new RedirectToActionResult("Accounts", "Admin", routeValues: null) : new RedirectToActionResult("Index", "Dashboard", routeValues: null);
}
