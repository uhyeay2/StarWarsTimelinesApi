using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages users' personal libraries of tracked source materials.
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// Gets the complete library of a user, including per-unit progress on each tracked source material.
    /// </summary>
    /// <param name="userId">The identifier of the user whose library is returned.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the user's library items.</returns>
    Task<IReadOnlyList<LibraryItemResponse>> GetLibraryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single library item for a user, including its per-unit progress.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <param name="sourceMaterialId">The identifier of the tracked source material.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The library item, or <c>null</c> when the source material is not tracked by the user.</returns>
    Task<LibraryItemResponse?> GetByIdAsync(Guid userId, Guid sourceMaterialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a source material to a user's library with an optional initial status (defaults to <see cref="TrackingStatus.WishListed"/>).
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the library.</param>
    /// <param name="sourceMaterialId">The identifier of the source material to track.</param>
    /// <param name="initialStatus">The initial tracking status, or <c>null</c> to default to <see cref="TrackingStatus.WishListed"/>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// The resulting library item, or <c>null</c> when the source material does not exist. When the item is already
    /// tracked, the existing item is returned unchanged.
    /// </returns>
    Task<LibraryItemResponse?> AddAsync(Guid userId, Guid sourceMaterialId, TrackingStatus? initialStatus = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates a library item's favorite flag and, for materials without season/volume sub-units, its
    /// status. For season/volume-based materials (shows and comics), the status is set by providing a
    /// <see cref="UpdateLibraryItemRequest.UnitId"/> targeting a Season/Volume unit.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <param name="sourceMaterialId">The identifier of the tracked source material.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated library item, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="BadRequestException">
    /// Thrown when <paramref name="request"/> sets a status on a source material that has season/volume sub-units
    /// without specifying a <see cref="UpdateLibraryItemRequest.UnitId"/>.
    /// </exception>
    Task<LibraryItemResponse?> UpdateAsync(
        Guid userId,
        Guid sourceMaterialId,
        UpdateLibraryItemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a source material from a user's library.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <param name="sourceMaterialId">The identifier of the tracked source material.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the item was removed; <c>false</c> when it did not exist.</returns>
    Task<bool> RemoveAsync(Guid userId, Guid sourceMaterialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders a user's library to match the given sequence of source material identifiers.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the library.</param>
    /// <param name="orderedSourceMaterialIds">
    /// The complete desired order of the library. Must contain every library item exactly once.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The re-ordered library.</returns>
    /// <exception cref="BadRequestException">
    /// Thrown when <paramref name="orderedSourceMaterialIds"/> does not contain exactly the user's library items,
    /// each exactly once.
    /// </exception>
    Task<IReadOnlyList<LibraryItemResponse>> ReorderAsync(
        Guid userId,
        IReadOnlyList<Guid> orderedSourceMaterialIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the requesting user's progress on a single unit of a tracked source material, creating the progress
    /// record on first change.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the library.</param>
    /// <param name="sourceMaterialId">The identifier of the tracked source material.</param>
    /// <param name="unitId">The identifier of the unit whose progress is set.</param>
    /// <param name="isCompleted">A value indicating whether the unit is completed.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// The updated unit with its progress, or <c>null</c> when the source material is not tracked by the user or the
    /// unit does not belong to the source material.
    /// </returns>
    Task<LibraryUnitResponse?> SetUnitProgressAsync(
        Guid userId,
        Guid sourceMaterialId,
        Guid unitId,
        bool isCompleted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the requesting user's progress on a single unit of a tracked source material, removing the
    /// progress record together with those of any child units (e.g. a season's episodes). When no progress
    /// rows remain for the source material afterwards, the library item itself is removed.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the library.</param>
    /// <param name="sourceMaterialId">The identifier of the tracked source material.</param>
    /// <param name="unitId">The identifier of the unit whose progress is cleared.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the request was applied (the library entry may have been removed with it); <c>false</c> when the item does not exist.</returns>
    /// <exception cref="BadRequestException">Thrown when the unit does not belong to the source material.</exception>
    Task<bool> ClearUnitProgressAsync(
        Guid userId,
        Guid sourceMaterialId,
        Guid unitId,
        CancellationToken cancellationToken = default);
}
