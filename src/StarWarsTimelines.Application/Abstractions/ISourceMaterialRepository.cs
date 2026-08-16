using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="SourceMaterial"/> catalog entries.
/// </summary>
public interface ISourceMaterialRepository
{
    /// <summary>
    /// Gets a single source material by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the source material.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="SourceMaterial"/>, or <c>null</c>.</returns>
    Task<SourceMaterial?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all source materials ordered alphabetically by title.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all source materials.</returns>
    Task<IReadOnlyList<SourceMaterial>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new source material for insertion.
    /// </summary>
    /// <param name="item">The source material to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(SourceMaterial item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing source material for update.
    /// </summary>
    /// <param name="item">The source material to update.</param>
    void Update(SourceMaterial item);

    /// <summary>
    /// Stages an existing source material for deletion.
    /// </summary>
    /// <param name="item">The source material to remove.</param>
    void Remove(SourceMaterial item);
}
