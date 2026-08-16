using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="SourceMaterialEvent"/> timeline entries.
/// </summary>
public interface ISourceMaterialEventRepository
{
    /// <summary>
    /// Gets all timeline events ordered by year and then title, with the source material and all linked
    /// characters, locations, and vehicles loaded.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all timeline events.</returns>
    Task<IReadOnlyList<SourceMaterialEvent>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single timeline event by its identifier, or <c>null</c> if no match is found, with the source
    /// material and all linked characters, locations, and vehicles loaded.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="SourceMaterialEvent"/>, or <c>null</c>.</returns>
    Task<SourceMaterialEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single timeline event by its identifier, or <c>null</c> if no match is found, tracked by the change
    /// tracker so its link collections can be edited before saving.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="SourceMaterialEvent"/>, or <c>null</c>.</returns>
    Task<SourceMaterialEvent?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new timeline event for insertion.
    /// </summary>
    /// <param name="item">The event to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(SourceMaterialEvent item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing timeline event for update.
    /// </summary>
    /// <param name="item">The event to update.</param>
    void Update(SourceMaterialEvent item);

    /// <summary>
    /// Stages an existing timeline event for deletion.
    /// </summary>
    /// <param name="item">The event to remove.</param>
    void Remove(SourceMaterialEvent item);
}
