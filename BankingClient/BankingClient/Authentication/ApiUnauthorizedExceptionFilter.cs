using BankingClient.Api;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankingClient.Authentication;

/// <summary>
/// When the API refuses the session's token, the cookie session is stale: sign it out and
/// send the user to log in again, instead of showing an error page. Registered globally,
/// so every page that calls the API gets this behavior without handling it itself.
/// </summary>
public sealed class ApiUnauthorizedExceptionFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiUnauthorizedException)
        {
            return;
        }

        var request = context.HttpContext.Request;
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Only a GET is safe to repeat after signing back in; a form post (like a transfer)
        // must never replay itself automatically.
        var returnUrl = HttpMethods.IsGet(request.Method) ? $"{request.PathBase}{request.Path}{request.QueryString}" : null;

        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, sessionExpired = true });
        context.ExceptionHandled = true;
    }
}
