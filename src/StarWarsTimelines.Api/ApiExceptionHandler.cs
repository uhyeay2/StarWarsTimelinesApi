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
        // Conflicts thrown by the services surface as a 409 Conflict so the client can tell the difference
        // between bad input and data that already exists or is still referenced: deleting a catalog entry
        // that is still referenced (ConflictException) and creating a duplicate of a unique value such as a
        // username, email address, or unit number (EntityAlreadyExistsException).
        if (exception is ConflictException)
        {
            await WriteProblemAsync(
                httpContext,
                exception.Message,
                StatusCodes.Status409Conflict,
                "Conflict",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                cancellationToken);
            return true;
        }

        // Invalid requests thrown by the services (missing or malformed fields, unknown referenced entities,
        // duplicates, unusable tokens) surface as a 400 Bad Request rather than a generic 500. The concrete
        // subtypes (EntityNotFoundException, EntityAlreadyExistsException, InvalidTokenException) all share
        // this mapping; their type names remain available to callers via the exception itself.
        if (exception is BadRequestException)
        {
            await WriteProblemAsync(
                httpContext,
                exception.Message,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                cancellationToken);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes an RFC 7807 problem-details response for an application exception.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="detail">The human-readable error description from the thrown exception.</param>
    /// <param name="statusCode">The HTTP status code to report.</param>
    /// <param name="title">The short problem summary.</param>
    /// <param name="type">The RFC 9110 problem-type URI.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private static async Task WriteProblemAsync(
        HttpContext httpContext,
        string detail,
        int statusCode,
        string title,
        string type,
        CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = httpContext.TraceIdentifier
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}
