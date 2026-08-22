namespace StarWarsTimelines.Application;

/// <summary>
/// Base class for application-thrown exceptions that carry a client-facing message and map to a
/// specific HTTP status code via <c>ApiExceptionHandler</c>.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="AppException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="paramName">The name of the request field that caused the error, or <c>null</c> when not tied to a single field.</param>
    protected AppException(string message, string? paramName = null)
        : base(message)
    {
        ParamName = paramName;
    }

    /// <summary>
    /// Gets the name of the request field that caused the error, or <c>null</c> when not tied to a single field.
    /// </summary>
    public string? ParamName { get; }
}
