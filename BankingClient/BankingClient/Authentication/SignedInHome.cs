using Microsoft.AspNetCore.Mvc;

namespace BankingClient.Authentication;

/// <summary>
/// A static class that provides a method to determine the appropriate home page for a signed-in user based on their role. If the user is an admin, they are redirected to the admin accounts page; otherwise, they are redirected to the customer dashboard.
/// </summary>
public static class SignedInHome
{
    public static RedirectToActionResult For(bool isAdmin) => isAdmin ? new RedirectToActionResult("Accounts", "Admin", routeValues: null) : new RedirectToActionResult("Index", "Dashboard", routeValues: null);
}
