using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="Location"/> catalog entries.
/// </summary>
public interface ILocationRepository
{
    /// <summary>
    /// Gets a single location by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the location.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="Location"/>, or <c>null</c>.</returns>
    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all locations ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all locations.</returns>
    Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new location for insertion.
    /// </summary>
    /// <param name="item">The location to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(Location item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing location for update.
    /// </summary>
    /// <param name="item">The location to update.</param>
    void Update(Location item);

    /// <summary>
    /// Stages an existing location for deletion.
    /// </summary>
    /// <param name="item">The location to remove.</param>
    void Remove(Location item);

    /// <summary>
    /// Determines whether any timeline event links the location.
    /// </summary>
    /// <param name="id">The unique identifier of the location.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the location is linked to at least one event; otherwise, <c>false</c>.</returns>
    Task<bool> IsReferencedByEventAsync(Guid id, CancellationToken cancellationToken = default);
}
