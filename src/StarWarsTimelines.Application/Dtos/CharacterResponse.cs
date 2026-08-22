using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents a character as returned by the API, including its optional biographical attributes.
/// </summary>
/// <param name="Id">The unique identifier of the character.</param>
/// <param name="Name">The character's name.</param>
/// <param name="PlanetBornOnId">
/// The unique identifier of the planet the character was born on, or <c>null</c> when it is unknown.
/// </param>
/// <param name="PlanetBornOnName">
/// The name of the planet the character was born on, or <c>null</c> when it is unknown.
/// </param>
/// <param name="YearOfBirthEarliest">
/// The chronologically earliest year the character could have been born in (negative for BBY, positive for ABY),
/// or <c>null</c> when unknown. Equals <paramref name="YearOfBirthLatest"/> for an exact birth year.
/// </param>
/// <param name="YearOfBirthLatest">
/// The chronologically latest year the character could have been born in (negative for BBY, positive for ABY),
/// or <c>null</c> when unknown. Equals <paramref name="YearOfBirthEarliest"/> for an exact birth year.
/// </param>
/// <param name="YearOfDeathEarliest">
/// The chronologically earliest year the character could have died in (negative for BBY, positive for ABY), or
/// <c>null</c> when unknown. Equals <paramref name="YearOfDeathLatest"/> for an exact death year.
/// </param>
/// <param name="YearOfDeathLatest">
/// The chronologically latest year the character could have died in (negative for BBY, positive for ABY), or
/// <c>null</c> when unknown. Equals <paramref name="YearOfDeathEarliest"/> for an exact death year.
/// </param>
/// <param name="SpeciesId">The unique identifier of the character's species, or <c>null</c> when it is unknown.</param>
/// <param name="SpeciesName">The character's species' name, or <c>null</c> when it is unknown.</param>
public record CharacterResponse(
    Guid Id,
    string Name,
    Guid? PlanetBornOnId,
    string? PlanetBornOnName,
    int? YearOfBirthEarliest,
    int? YearOfBirthLatest,
    int? YearOfDeathEarliest,
    int? YearOfDeathLatest,
    Guid? SpeciesId,
    string? SpeciesName)
{
    /// <summary>
    /// Maps a <see cref="Character"/> entity to a response DTO.
    /// </summary>
    /// <param name="item">The character entity to map.</param>
    /// <returns>A <see cref="CharacterResponse"/> populated from the entity.</returns>
    public static CharacterResponse FromEntity(Character item) => new(
        item.Id,
        item.Name,
        item.PlanetBornOnId,
        item.PlanetBornOn?.Name,
        item.YearOfBirthEarliest,
        item.YearOfBirthLatest,
        item.YearOfDeathEarliest,
        item.YearOfDeathLatest,
        item.SpeciesId,
        item.Species?.Name);
}
