using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages the vehicle catalog.
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Gets all vehicles ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all vehicles.</returns>
    Task<IReadOnlyList<VehicleResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single vehicle by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the vehicle.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching vehicle, or <c>null</c>.</returns>
    Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new vehicle in the catalog.
    /// </summary>
    /// <param name="request">The payload describing the new vehicle.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created vehicle.</returns>
    /// <exception cref="BadRequestException">Thrown when a non-null request name is null or white space.</exception>
    Task<VehicleResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing vehicle's name.
    /// </summary>
    /// <param name="id">The unique identifier of the vehicle to update.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated vehicle, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="BadRequestException">Thrown when a non-null request name is null or white space.</exception>
    Task<VehicleResponse?> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a vehicle from the catalog.
    /// </summary>
    /// <param name="id">The unique identifier of the vehicle to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the vehicle was deleted; <c>false</c> when it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
