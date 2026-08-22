using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages the source material catalog.
/// </summary>
public interface ISourceMaterialService
{
    /// <summary>
    /// Gets all source materials ordered alphabetically by title.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all source materials.</returns>
    Task<IReadOnlyList<SourceMaterialResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single source material by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the source material.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching source material, or <c>null</c>.</returns>
    Task<SourceMaterialResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new source material in the catalog.
    /// </summary>
    /// <param name="request">The payload describing the new source material.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created source material.</returns>
    /// <exception cref="BadRequestException">Thrown when the request title is null or white space.</exception>
    Task<SourceMaterialResponse> CreateAsync(CreateSourceMaterialRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates an existing source material.
    /// </summary>
    /// <param name="id">The unique identifier of the source material to update.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated source material, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="BadRequestException">Thrown when a non-null request title is null or white space.</exception>
    Task<SourceMaterialResponse?> UpdateAsync(Guid id, UpdateSourceMaterialRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a source material from the catalog.
    /// </summary>
    /// <param name="id">The unique identifier of the source material to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the source material was deleted; <c>false</c> when it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
