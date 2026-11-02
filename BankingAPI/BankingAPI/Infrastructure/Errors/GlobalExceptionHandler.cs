using BankingAPI.Domain;
using BankingAPI.Domain.Accounts;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Infrastructure.Errors;

/// <summary>
/// Turns exceptions into RFC 7807 responses. Domain exceptions carry messages written for
/// customers and map to 404 or 422. Anything else is a bug: it's logged and returned as a
/// generic 500, so internal details never reach the caller.
/// </summary>
internal sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            AccountNotFoundException notFound => (StatusCodes.Status404NotFound, "Account not found", notFound.Message),
            BusinessRuleException ruleBroken => (StatusCodes.Status422UnprocessableEntity, "Request could not be completed", ruleBroken.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
