namespace StarWarsTimelines.Application;

/// <summary>
/// Thrown when an operation would create a duplicate of an entity that must be unique, such as
/// registering a username that is already taken. Maps to 409 Conflict per RESTful semantics.
/// </summary>
public sealed class EntityAlreadyExistsException : ConflictException
{
    /// <summary>
    /// Creates a new instance of the <see cref="EntityAlreadyExistsException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the duplicate.</param>
    /// <param name="paramName">The name of the request field that carries the duplicate value, or <c>null</c>.</param>
    public EntityAlreadyExistsException(string message, string? paramName = null)
        : base(message, paramName)
    {
    }
}
