using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Provides data access for <see cref="Character"/> catalog entries.
/// </summary>
public interface ICharacterRepository
{
    /// <summary>
    /// Gets a single character by its identifier, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="id">The unique identifier of the character.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching <see cref="Character"/>, or <c>null</c>.</returns>
    Task<Character?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all characters ordered alphabetically by name.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all characters.</returns>
    Task<IReadOnlyList<Character>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new character for insertion.
    /// </summary>
    /// <param name="item">The character to add.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AddAsync(Character item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an existing character for update.
    /// </summary>
    /// <param name="item">The character to update.</param>
    void Update(Character item);

    /// <summary>
    /// Stages an existing character for deletion.
    /// </summary>
    /// <param name="item">The character to remove.</param>
    void Remove(Character item);
}
