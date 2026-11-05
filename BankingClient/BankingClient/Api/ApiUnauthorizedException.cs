namespace BankingClient.Api;

/// <summary>
/// An exception that is thrown when the banking API rejects the session's access token, indicating that the user is not authorized to perform the requested operation. This exception can be used to handle unauthorized access scenarios in the client application, such as prompting the user to log in again or displaying an appropriate error message.
/// </summary>
public sealed class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The API rejected the session's access token.") { }


    public ApiUnauthorizedException(string message) : base(message) { }


    public ApiUnauthorizedException(string message, Exception innerException) : base(message, innerException) { }

}
