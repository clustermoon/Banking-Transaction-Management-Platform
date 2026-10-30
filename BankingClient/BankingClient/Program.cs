using BankingClient.Api;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankingClient;

/// <summary>
/// Configures and starts the BankingClient web application.
/// The client maintains a browser cookie session while delegating authentication
/// and token validation to the Banking API.
/// </summary>
public sealed class Program
{
    private Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register the typed API client and its delegating handler. The handler copies
        // the access token from the authenticated browser session onto API requests.
        builder.Services.AddOptions<BankingApiOptions>().BindConfiguration(BankingApiOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services.AddHttpClient<IBankingApiClient, BankingApiClient>(
            (serviceProvider, httpClient) => httpClient.BaseAddress = serviceProvider.GetRequiredService<IOptions<BankingApiOptions>>().Value.BaseUrl
        ).AddHttpMessageHandler<BearerTokenHandler>();

        // The cookie represents the client session; it does not replace the API token.
        // Disable sliding expiration so the browser session cannot outlive that token.
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "avenor.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;

            // The demo runs over plain HTTP in Docker; behind HTTPS the cookie is marked Secure.
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/account/login";

            // The session ends when the API token does; an expired token can't be renewed here.
            options.SlidingExpiration = false;
        });

        // Configure MVC routing and enforce antiforgery validation for unsafe HTTP methods.
        // This protects form posts, including login, registration, and logout.
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        // Authentication must run before authorization so [Authorize] actions can evaluate
        // the session cookie before the request reaches a controller.
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

        await app.RunAsync();
    }
}
