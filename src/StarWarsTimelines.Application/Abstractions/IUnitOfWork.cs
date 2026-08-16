namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Encapsulates a unit of work that commits all pending database changes together.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes staged in the current unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
