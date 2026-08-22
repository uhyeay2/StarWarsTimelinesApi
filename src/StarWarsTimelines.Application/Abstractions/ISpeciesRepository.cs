using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="Species"/> catalog entries.
/// </summary>
public interface ISpeciesRepository
{
    /// <summary>
    /// Gets a single species by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the species.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="Species"/>, or <c>null</c>.</returns>
    Task<Species?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all species ordered alphabetically by name, including their home planets.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all species.</returns>
    Task<IReadOnlyList<Species>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new species for insertion.
    /// </summary>
    /// <param name="item">The species to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(Species item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing species for update.
    /// </summary>
    /// <param name="item">The species to update.</param>
    void Update(Species item);

    /// <summary>
    /// Stages an existing species for deletion.
    /// </summary>
    /// <param name="item">The species to remove.</param>
    void Remove(Species item);
}
