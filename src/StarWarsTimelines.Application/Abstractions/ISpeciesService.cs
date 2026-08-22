using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides operations for managing the species catalog.
/// </summary>
public interface ISpeciesService
{
    /// <summary>
    /// Gets all species ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>All catalogued species.</returns>
    Task<IReadOnlyList<SpeciesResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single species by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the species.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching species, or <c>null</c> when no species has the identifier.</returns>
    Task<SpeciesResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new species in the catalog.
    /// </summary>
    /// <param name="request">The payload describing the species to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created species.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the name is blank or the referenced home planet does not exist.
    /// </exception>
    Task<SpeciesResponse> CreateAsync(CreateSpeciesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates an existing species. Properties left <c>null</c> are unchanged.
    /// </summary>
    /// <param name="id">The unique identifier of the species.</param>
    /// <param name="request">The payload describing the changes to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated species, or <c>null</c> when no species has the identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the name is blank or the referenced home planet does not exist.
    /// </exception>
    Task<SpeciesResponse?> UpdateAsync(Guid id, UpdateSpeciesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a species from the catalog. Characters referencing it keep their identity with the species
    /// reference cleared to unknown.
    /// </summary>
    /// <param name="id">The unique identifier of the species.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the species was deleted; <c>false</c> when no species has the identifier.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
