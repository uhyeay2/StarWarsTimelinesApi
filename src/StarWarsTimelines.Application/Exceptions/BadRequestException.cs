namespace StarWarsTimelines.Application;

/// <summary>
/// Thrown when a request is invalid: required fields are missing or malformed, values are
/// inconsistent with each other, or an operation is applied to the wrong target.
/// </summary>
public class BadRequestException : AppException
{
    /// <summary>
    /// Creates a new instance of the <see cref="BadRequestException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the validation failure.</param>
    /// <param name="paramName">The name of the request field that caused the error, or <c>null</c>.</param>
    public BadRequestException(string message, string? paramName = null)
        : base(message, paramName)
    {
    }
}
