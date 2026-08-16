using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages the source material unit catalog.
/// </summary>
public interface ISourceMaterialUnitService
{
    /// <summary>
    /// Gets all units of a source material ordered by number.
    /// </summary>
    /// <param name="sourceMaterialId">The identifier of the source material whose units are returned.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The material's units, or <c>null</c> when the source material does not exist.</returns>
    Task<IReadOnlyList<SourceMaterialUnitResponse>?> GetBySourceMaterialAsync(Guid sourceMaterialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new unit for a source material.
    /// </summary>
    /// <param name="sourceMaterialId">The identifier of the source material the unit belongs to.</param>
    /// <param name="request">The unit data to create.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created unit, or <c>null</c> when the source material does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when the unit number is invalid or already in use.</exception>
    Task<SourceMaterialUnitResponse?> CreateAsync(Guid sourceMaterialId, CreateSourceMaterialUnitRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates a source material unit.
    /// </summary>
    /// <param name="id">The identifier of the unit to update.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated unit, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when the updated number is invalid or already in use.</exception>
    Task<SourceMaterialUnitResponse?> UpdateAsync(Guid id, UpdateSourceMaterialUnitRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a source material unit.
    /// </summary>
    /// <param name="id">The identifier of the unit to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the unit was deleted; <c>false</c> when it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
