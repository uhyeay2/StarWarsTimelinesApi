namespace StarWarsTimelines.Application;

/// <summary>
/// Thrown when an operation references an entity that does not exist, such as linking a timeline event
/// to an unknown character. Maps to 400 Bad Request because the error stems from invalid request input.
/// </summary>
public sealed class EntityNotFoundException : BadRequestException
{
    /// <summary>
    /// Creates a new instance of the <see cref="EntityNotFoundException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the missing entity.</param>
    /// <param name="paramName">The name of the request field that referenced the entity, or <c>null</c>.</param>
    public EntityNotFoundException(string message, string? paramName = null)
        : base(message, paramName)
    {
    }
}
