namespace StarWarsTimelines.Application;

/// <summary>
/// Thrown when a security token presented by the caller is unusable: unknown, expired, revoked, or
/// no longer linked to an existing account. Maps to 400 Bad Request.
/// </summary>
public sealed class InvalidTokenException : BadRequestException
{
    /// <summary>
    /// Creates a new instance of the <see cref="InvalidTokenException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of why the token was rejected.</param>
    /// <param name="paramName">The name of the field carrying the token, or <c>null</c>.</param>
    public InvalidTokenException(string message, string? paramName = null)
        : base(message, paramName)
    {
    }
}
