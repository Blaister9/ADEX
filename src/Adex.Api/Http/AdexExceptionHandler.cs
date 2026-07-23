using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Adex.Api.Http;

/// <summary>
/// Turns every unhandled exception into a problem+json response that follows the
/// public contract.
///
/// Two rules:
/// <list type="bullet">
///   <item>a malformed request is the client's problem and answers <c>400</c>,
///   not <c>500</c> — an unreadable body is not a server fault;</item>
///   <item>anything else answers a generic <c>500</c> whose body never contains
///   an exception message, a stack trace, SQL or an internal identifier. The
///   detail a support engineer needs is the correlation id, which is in the
///   response and in the log.</item>
/// </list>
/// </summary>
public sealed class AdexExceptionHandler(ILogger<AdexExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        string correlationId = CorrelationIdMiddleware.Current(httpContext);

        (int status, string type, string title, string detail) = exception switch
        {
            BadHttpRequestException badRequest => (
                badRequest.StatusCode,
                Problems.MalformedRequest,
                "The request could not be read.",
                "The request body could not be parsed as JSON matching this endpoint's contract."),
            _ => (
                StatusCodes.Status500InternalServerError,
                Problems.DependencyUnavailable,
                "The request could not be completed.",
                "ADEX could not complete this request. Quote the correlation id when reporting it."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path} (correlation {CorrelationId}).",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }
        else
        {
            logger.LogInformation(
                "Rejected malformed request for {Method} {Path} (correlation {CorrelationId}).",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["correlation_id"] = correlationId;

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response
            .WriteAsJsonAsync(problem, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
