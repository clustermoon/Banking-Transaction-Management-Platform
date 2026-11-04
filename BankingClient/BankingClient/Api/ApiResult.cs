namespace BankingClient.Api;

/// <summary>
/// A generic result type for API operations, encapsulating either a successful value of type <typeparamref name="T"/> or an <see cref="ApiProblem"/> indicating a failure. This record is used to represent the outcome of API calls, allowing the caller to handle both success and error cases in a consistent manner.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Value"></param>
/// <param name="Problem"></param>
public sealed record ApiResult<T>(T? Value, ApiProblem? Problem) where T : class;
