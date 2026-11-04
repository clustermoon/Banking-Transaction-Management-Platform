using BankingClient.Api.Contracts;

namespace BankingClient.Api;

public interface IBankingApiClient
{
    Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<ApiProblem?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<ProfileResponse> GetProfileAsync(string? accessToken, CancellationToken cancellationToken);

    Task LogoutAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(CancellationToken cancellationToken);

    Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<ApiResult<AccountHistoryResponse>> GetHistoryAsync(Guid accountId, DateOnly? from, DateOnly? onto, CancellationToken cancellationToken);

    Task<ApiResult<AccountResponse>> OpenAccountAsync(OpenAccountRequest request, CancellationToken cancellationToken);

    Task<ApiResult<MoneyMovementResult>> DepositAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken);

    Task<ApiResult<MoneyMovementResult>> WithdrawAsync(Guid accountId, MoneyMovementRequest request, Guid idempotencyKey, CancellationToken cancellationToken);

    Task<ApiResult<MoneyMovementResult>> TransferAsync(Guid fromAccountId, TransferMoneyRequest request, Guid idempotencyKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminAccountResponse>> GetAdminAccountsAsync(CancellationToken cancellationToken);

    Task<AdminAccountResponse?> GetAdminAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditEntryResponse>> GetAuditLogAsync(Guid? accountId, CancellationToken cancellationToken);

    Task<ApiResult<AuditEntryResponse>> ChangeAccountStatusAsync(Guid accountId, AccountStatus toStatus, string reason, CancellationToken cancellationToken);

}
