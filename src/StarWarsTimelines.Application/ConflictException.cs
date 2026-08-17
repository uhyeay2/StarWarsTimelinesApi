namespace StarWarsTimelines.Application;

/// <summary>
/// Thrown when an operation cannot complete because the target is still referenced by other data,
/// such as deleting a catalog entry that is linked to timeline events or user libraries.
/// </summary>
public sealed class ConflictException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="ConflictException"/>.
    /// </summary>
    /// <param name="message">A human-readable description of the conflict.</param>
    public ConflictException(string message)
        : base(message)
    {
    }
}
