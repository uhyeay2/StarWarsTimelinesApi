using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="UserSourceMaterialUnit"/> per-user progress records.
/// </summary>
public interface IUserSourceMaterialUnitRepository
{
    /// <summary>
    /// Gets all of a user's unit progress records.
    /// </summary>
    /// <param name="userId">The identifier of the user whose progress is returned.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of the user's progress records.</returns>
    Task<IReadOnlyList<UserSourceMaterialUnit>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single progress record for a user and unit, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="userId">The identifier of the owning user.</param>
    /// <param name="sourceMaterialUnitId">The identifier of the tracked unit.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="UserSourceMaterialUnit"/>, or <c>null</c>.</returns>
    Task<UserSourceMaterialUnit?> GetByIdAsync(Guid userId, Guid sourceMaterialUnitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new progress record for insertion.
    /// </summary>
    /// <param name="item">The progress record to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(UserSourceMaterialUnit item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing progress record for update.
    /// </summary>
    /// <param name="item">The progress record to update.</param>
    void Update(UserSourceMaterialUnit item);

    /// <summary>
    /// Stages the given progress records for deletion.
    /// </summary>
    /// <param name="items">The progress records to remove.</param>
    void RemoveRange(IEnumerable<UserSourceMaterialUnit> items);
}
