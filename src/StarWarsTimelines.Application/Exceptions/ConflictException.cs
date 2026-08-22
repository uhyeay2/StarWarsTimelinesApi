namespace StarWarsTimelines.Application;

/// <summary>
/// Thrown when an operation conflicts with existing data: either the target is still referenced by other
/// data (such as deleting a catalog entry that is linked to timeline events or user libraries) or it would
/// create a duplicate of an entity that must be unique (see <see cref="EntityAlreadyExistsException"/>).
/// </summary>
public class ConflictException : AppException
{
    /// <summary>
    /// Creates a new instance of the <see cref="ConflictException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the conflict.</param>
    /// <param name="paramName">The name of the request field that caused the error, or <c>null</c> when not tied to a single field.</param>
    public ConflictException(string message, string? paramName = null)
        : base(message, paramName)
    {
    }
}
