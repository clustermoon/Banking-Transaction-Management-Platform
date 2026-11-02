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

    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(CancellationToken cancellationToken);

    /// <returns>Null when the account doesn't exist or belongs to someone else.</returns>
    Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<ApiResult<AccountHistoryResponse>> GetHistoryAsync(Guid accountId, DateOnly? from, DateOnly? onto, CancellationToken cancellationToken);

    Task<ApiResult<AccountResponse>> OpenAccountAsync(OpenAccountRequest request, CancellationToken cancellationToken);

    Task<ApiResult<MoneyMovementResult>> DepositAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken);

    Task<ApiResult<MoneyMovementResult>> WithdrawAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken);

    Task<ApiResult<MoneyMovementResult>> TransferAsync(Guid fromAccountId, TransferMoneyRequest request, Guid idempotencyKey, CancellationToken cancellationToken);
}
