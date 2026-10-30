namespace BankingClient.Api;

/// <summary>
/// The API refused the session's access token: it expired, or the user signed out elsewhere,
/// which revokes every token. Handled globally by ApiUnauthorizedExceptionFilter.
/// </summary>
public sealed class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The API rejected the session's access token.") { }


    public ApiUnauthorizedException(string message) : base(message) { }


    public ApiUnauthorizedException(string message, Exception innerException) : base(message, innerException) { }

}
