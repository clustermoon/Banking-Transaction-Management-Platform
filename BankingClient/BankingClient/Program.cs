using BankingClient.Api;
using BankingClient.Authentication;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankingClient;

/// <summary>
/// Starts the web application. The app is a thin client that authenticates the user and forwards requests to the API.
/// </summary>
public sealed class Program
{
    private Program()
    {
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Service configuration For the API client and authentication
        builder.Services.AddOptions<BankingApiOptions>().BindConfiguration(BankingApiOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services.AddHttpClient<IBankingApiClient, BankingApiClient>(
            (serviceProvider, httpClient) => httpClient.BaseAddress = serviceProvider.GetRequiredService<IOptions<BankingApiOptions>>().Value.BaseUrl
        ).AddHttpMessageHandler<BearerTokenHandler>();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "avenor.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/error/403";

            options.SlidingExpiration = false;
        });

        // Configure MVC and routing
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.Filters.Add<ApiUnauthorizedExceptionFilter>();
        });

        // Configure the app and middleware
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }
        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.UseRequestLocalization("en-US");

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

        await app.RunAsync();
    }
}
