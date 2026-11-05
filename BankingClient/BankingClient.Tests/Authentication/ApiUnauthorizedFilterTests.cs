using BankingClient.Api;
using BankingClient.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace BankingClient.Tests.Authentication;

/// <summary>
/// This class contains unit tests for the ApiUnauthorizedExceptionFilter, which is responsible for handling ApiUnauthorizedException instances thrown during API calls. The tests verify that the filter correctly signs out the user and redirects them to the login page when an unauthorized exception occurs, and that it does not interfere with other types of exceptions.
/// </summary>
public sealed class ApiUnauthorizedExceptionFilterTests
{
    /// <summary>
    /// Creates a test context for the ApiUnauthorizedExceptionFilter, including a mocked IAuthenticationService and an ExceptionContext with the specified exception, HTTP method, and request path. This setup allows for testing how the filter handles different scenarios without requiring actual HTTP requests or authentication services.
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="method"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private static (ExceptionContext Context, Mock<IAuthenticationService> Authentication) CreateContext(
        Exception exception, string method, string path)
    {
        var authentication = new Mock<IAuthenticationService>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(authentication.Object)
                .BuildServiceProvider(),
        };
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return (new ExceptionContext(actionContext, []) { Exception = exception }, authentication);
    }

    #region FACT? - Revoked Session Is Signed Out And Sent To Login With Its Page
    [Fact]
    public async Task RevokedSessionIsSignedOutAndSentToLoginWithItsPage()
    {
        var (context, authentication) = CreateContext(new ApiUnauthorizedException(), HttpMethods.Get, "/profile");

        await new ApiUnauthorizedExceptionFilter().OnExceptionAsync(context);

        authentication.Verify(
            service => service.SignOutAsync(
                context.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties?>()),
            Times.Once);
        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("/profile", redirect.RouteValues!["returnUrl"]);
        Assert.Equal(true, redirect.RouteValues["sessionExpired"]);
        Assert.True(context.ExceptionHandled);
    }
    #endregion

    #region FACT? - Revoked Form Post Is Not Replayed After Signing In
    [Fact]
    public async Task RevokedFormPostIsNotReplayedAfterSigningIn()
    {
        var (context, _) = CreateContext(new ApiUnauthorizedException(), HttpMethods.Post, "/profile");

        await new ApiUnauthorizedExceptionFilter().OnExceptionAsync(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Null(redirect.RouteValues!["returnUrl"]);
    }
    #endregion

    #region FACT? - Other Exceptions Are Left For The Error Page
    [Fact]
    public async Task OtherExceptionsAreLeftForTheErrorPage()
    {
        var (context, authentication) = CreateContext(new InvalidOperationException(), HttpMethods.Get, "/profile");

        await new ApiUnauthorizedExceptionFilter().OnExceptionAsync(context);

        Assert.Null(context.Result);
        Assert.False(context.ExceptionHandled);
        authentication.VerifyNoOtherCalls();
    }
    #endregion
}
