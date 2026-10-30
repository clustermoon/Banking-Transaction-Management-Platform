using BankingClient.Api.Contracts;

namespace BankingClient.Api;

public interface IBankingApiClient
{
    Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <returns>Null on success; otherwise the API's validation problem.</returns>
    Task<ApiProblem?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <param name="accessToken">
    /// Pass during sign-in, before the session cookie exists. Otherwise null: the signed-in
    /// user's token is attached by <see cref="BearerTokenHandler"/>.
    /// </param>
    Task<ProfileResponse> GetProfileAsync(string? accessToken, CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);
}
