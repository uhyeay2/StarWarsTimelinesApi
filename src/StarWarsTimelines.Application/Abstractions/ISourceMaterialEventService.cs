using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages timeline events and their links to characters, locations, and vehicles.
/// </summary>
public interface ISourceMaterialEventService
{
    /// <summary>
    /// Gets all timeline events ordered by year and then title.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all timeline events.</returns>
    Task<IReadOnlyList<SourceMaterialEventResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single timeline event by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching event, or <c>null</c>.</returns>
    Task<SourceMaterialEventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new timeline event referencing an existing source material and links.
    /// </summary>
    /// <param name="request">The payload describing the new event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created event with its linked entities.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the title is null or white space, or when the source material or any linked character, location,
    /// or vehicle does not exist.
    /// </exception>
    Task<SourceMaterialEventResponse> CreateAsync(CreateSourceMaterialEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates an existing timeline event and its links.
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated event, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a non-null title is null or white space, or when a referenced source material, character,
    /// location, or vehicle does not exist.
    /// </exception>
    Task<SourceMaterialEventResponse?> UpdateAsync(Guid id, UpdateSourceMaterialEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a timeline event.
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the event was deleted; <c>false</c> when it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
