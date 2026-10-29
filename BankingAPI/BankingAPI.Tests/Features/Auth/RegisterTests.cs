using System.Net;
using System.Net.Http.Json;

using BankingAPI.Features.Auth;
using BankingAPI.Identity;
using BankingAPI.Infrastructure;
using BankingAPI.Tests.Infrastructure;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BankingAPI.Tests.Features.Auth;

public sealed class RegisterTests(BankingApiFactory factory)
{
    private static readonly Uri RegisterUri = new("/api/auth/register", UriKind.Relative);

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.test";

    [Fact]
    public async Task RegisterCreatesCustomer()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var request = new RegisterRequest(UniqueEmail(), "a-long-enough-password", "Test User");

        using var response = await client.PostAsJsonAsync(RegisterUri, request, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(request.Email);
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, Roles.Customer));
    }

    [Theory]
    [InlineData("not-an-email", "a-long-enough-password", "Email")]
    [InlineData("valid@example.test", "short", "Password")]
    public async Task RegisterRejectsInvalidInput(string email, string password, string expectedField)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            RegisterUri, new RegisterRequest(email, password, "Test User"), cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        Assert.Contains(problem!.Errors.Keys, key => key.Equals(expectedField, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterRejectsDuplicateEmail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var request = new RegisterRequest(UniqueEmail(), "a-long-enough-password", "Test User");

        using var first = await client.PostAsJsonAsync(RegisterUri, request, cancellationToken);
        using var second = await client.PostAsJsonAsync(RegisterUri, request, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}
