using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Application service that manages the character catalog.
/// </summary>
public interface ICharacterService
{
    /// <summary>
    /// Gets all characters ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all characters.</returns>
    Task<IReadOnlyList<CharacterResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single character by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the character.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching character, or <c>null</c>.</returns>
    Task<CharacterResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new character in the catalog.
    /// </summary>
    /// <param name="request">The payload describing the new character.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The created character.</returns>
    /// <exception cref="ArgumentException">Thrown when the request name is null or white space.</exception>
    Task<CharacterResponse> CreateAsync(CreateCharacterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing character's name.
    /// </summary>
    /// <param name="id">The unique identifier of the character to update.</param>
    /// <param name="request">The fields to change; null fields are left unchanged.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The updated character, or <c>null</c> if it does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when the request name is null or white space.</exception>
    Task<CharacterResponse?> UpdateAsync(Guid id, UpdateCharacterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a character from the catalog.
    /// </summary>
    /// <param name="id">The unique identifier of the character to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><c>true</c> when the character was deleted; <c>false</c> when it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
