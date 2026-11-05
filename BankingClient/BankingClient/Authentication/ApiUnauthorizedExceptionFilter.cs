using BankingClient.Api;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankingClient.Authentication;

/// <summary>
/// An exception filter that handles <see cref="ApiUnauthorizedException"/> thrown by the banking API client. When such an exception occurs, the filter signs the user out of their current session and redirects them to the login page, optionally preserving the original request URL for a return after re-authentication. This ensures that users are prompted to log in again if their session has expired or if they are unauthorized to access a resource.
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

        var returnUrl = HttpMethods.IsGet(request.Method) ? $"{request.PathBase}{request.Path}{request.QueryString}" : null;

        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, sessionExpired = true });
        context.ExceptionHandled = true;
    }
}
