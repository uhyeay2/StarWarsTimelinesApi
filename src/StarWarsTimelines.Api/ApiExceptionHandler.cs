using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StarWarsTimelines.Application;

namespace StarWarsTimelines.Api;

/// <summary>
/// Maps exceptions thrown by application services to appropriate HTTP responses.
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Validation errors thrown by the services surface as ArgumentException and should be reported as a 400
        // Bad Request rather than a generic 500.
        if (exception is ArgumentException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = exception.Message,
                Instance = httpContext.TraceIdentifier
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        // Conflicts thrown by the services (deleting a catalog entry that is still referenced) surface as a 409
        // Conflict so the client can tell the difference between bad input and a data dependency.
        if (exception is ConflictException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Detail = exception.Message,
                Instance = httpContext.TraceIdentifier
            };

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        return false;
    }
}
