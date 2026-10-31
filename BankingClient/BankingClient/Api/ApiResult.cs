namespace BankingClient.Api;

/// <summary>
/// The outcome of a call whose failure is an expected result (wrong password, invalid input)
/// rather than an error. Unexpected failures throw instead.
/// </summary>
public sealed record ApiResult<T>(T? Value, ApiProblem? Problem) where T : class;
