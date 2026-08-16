using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="UserSourceMaterial"/> library items.
/// </summary>
public interface IUserSourceMaterialRepository
{
    /// <summary>
    /// Gets the complete library of a user, ordered by the user's sort order and then by when each item was added,
    /// with the tracked <see cref="UserSourceMaterial.SourceMaterial"/> navigation loaded.
    /// </summary>
    /// <param name="userId">The identifier of the user whose library is returned.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the user's library items.</returns>
    Task<IReadOnlyList<UserSourceMaterial>> GetLibraryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete library of a user as tracked entities without their navigations loaded, so the items can be
    /// updated in place. This is used by reorder operations.
    /// </summary>
    /// <param name="userId">The identifier of the user whose library is returned.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the user's tracked library items.</returns>
    Task<IReadOnlyList<UserSourceMaterial>> GetTrackedItemsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next sort order to assign to a newly added library item, based on the highest current sort order.
    /// </summary>
    /// <param name="userId">The identifier of the user whose library is queried.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The next available sort order value.</returns>
    Task<int> GetNextSortOrderAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single library item for a user and source material, with the tracked
    /// <see cref="UserSourceMaterial.SourceMaterial"/> navigation loaded, or <c>null</c> if it does not exist.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <param name="sourceMaterialId">The identifier of the tracked source material.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="UserSourceMaterial"/>, or <c>null</c>.</returns>
    Task<UserSourceMaterial?> GetByIdAsync(Guid userId, Guid sourceMaterialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new library item for insertion.
    /// </summary>
    /// <param name="item">The library item to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(UserSourceMaterial item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing library item for update.
    /// </summary>
    /// <param name="item">The library item to update.</param>
    void Update(UserSourceMaterial item);

    /// <summary>
    /// Stages an existing library item for deletion.
    /// </summary>
    /// <param name="item">The library item to remove.</param>
    void Remove(UserSourceMaterial item);
}
