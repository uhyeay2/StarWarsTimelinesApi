using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages the location catalog.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Gets all locations ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all locations.</returns>
    Task<IReadOnlyList<LocationResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single location by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the location.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching location, or <c>null</c>.</returns>
    Task<LocationResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new location in the catalog.
    /// </summary>
    /// <param name="request">The payload describing the new location.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created location.</returns>
    /// <exception cref="ArgumentException">Thrown when the request name is null or white space.</exception>
    Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing location's name.
    /// </summary>
    /// <param name="id">The unique identifier of the location to update.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated location, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when the request name is null or white space.</exception>
    Task<LocationResponse?> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a location from the catalog.
    /// </summary>
    /// <param name="id">The unique identifier of the location to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the location was deleted; <c>false</c> when it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
