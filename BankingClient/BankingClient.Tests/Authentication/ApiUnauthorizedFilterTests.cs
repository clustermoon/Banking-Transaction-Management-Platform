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

public sealed class ApiUnauthorizedExceptionFilterTests
{
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

    [Fact]
    public async Task RevokedFormPostIsNotReplayedAfterSigningIn()
    {
        var (context, _) = CreateContext(new ApiUnauthorizedException(), HttpMethods.Post, "/profile");

        await new ApiUnauthorizedExceptionFilter().OnExceptionAsync(context);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Null(redirect.RouteValues!["returnUrl"]);
    }

    [Fact]
    public async Task OtherExceptionsAreLeftForTheErrorPage()
    {
        var (context, authentication) = CreateContext(new InvalidOperationException(), HttpMethods.Get, "/profile");

        await new ApiUnauthorizedExceptionFilter().OnExceptionAsync(context);

        Assert.Null(context.Result);
        Assert.False(context.ExceptionHandled);
        authentication.VerifyNoOtherCalls();
    }
}
