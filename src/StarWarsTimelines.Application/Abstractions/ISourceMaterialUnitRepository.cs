using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="SourceMaterialUnit"/> catalog entries.
/// </summary>
public interface ISourceMaterialUnitRepository
{
    /// <summary>
    /// Gets a single unit by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the unit.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="SourceMaterialUnit"/>, or <c>null</c>.</returns>
    Task<SourceMaterialUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all units of a source material ordered by group and then number.
    /// </summary>
    /// <param name="sourceMaterialId">The identifier of the source material whose units are returned.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the material's units, ordered by group and then number.</returns>
    Task<IReadOnlyList<SourceMaterialUnit>> GetBySourceMaterialAsync(Guid sourceMaterialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single unit of a source material by its group and number, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="sourceMaterialId">The identifier of the owning source material.</param>
    /// <param name="groupNumber">The group (season/volume) the unit belongs to, or <c>null</c> for ungrouped units.</param>
    /// <param name="number">The unit number to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="SourceMaterialUnit"/>, or <c>null</c>.</returns>
    Task<SourceMaterialUnit?> GetByNumberAsync(Guid sourceMaterialId, int? groupNumber, int number, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new unit for insertion.
    /// </summary>
    /// <param name="item">The unit to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(SourceMaterialUnit item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing unit for update.
    /// </summary>
    /// <param name="item">The unit to update.</param>
    void Update(SourceMaterialUnit item);

    /// <summary>
    /// Stages an existing unit for deletion.
    /// </summary>
    /// <param name="item">The unit to remove.</param>
    void Remove(SourceMaterialUnit item);

    /// <summary>
    /// Determines whether the unit is referenced by a timeline event or by a user's unit progress.
    /// </summary>
    /// <param name="id">The unique identifier of the unit.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the unit is still referenced; otherwise, <c>false</c>.</returns>
    Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken = default);
}
