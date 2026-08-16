using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="Vehicle"/> catalog entries.
/// </summary>
public interface IVehicleRepository
{
    /// <summary>
    /// Gets a single vehicle by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the vehicle.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="Vehicle"/>, or <c>null</c>.</returns>
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all vehicles ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all vehicles.</returns>
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new vehicle for insertion.
    /// </summary>
    /// <param name="item">The vehicle to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(Vehicle item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing vehicle for update.
    /// </summary>
    /// <param name="item">The vehicle to update.</param>
    void Update(Vehicle item);

    /// <summary>
    /// Stages an existing vehicle for deletion.
    /// </summary>
    /// <param name="item">The vehicle to remove.</param>
    void Remove(Vehicle item);
}
