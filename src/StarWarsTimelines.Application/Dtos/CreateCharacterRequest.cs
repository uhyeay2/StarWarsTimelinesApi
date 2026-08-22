namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to create a new character in the catalog. All biographical attributes are
/// optional because they are unknown for many characters.
/// </summary>
/// <param name="Name">The character's name.</param>
/// <param name="PlanetBornOnId">
/// The identifier of the planet the character was born on, or <c>null</c> when it is unknown.
/// </param>
/// <param name="YearOfBirthEarliest">
/// The chronologically earliest year the character could have been born in (negative for BBY, positive for ABY),
/// or <c>null</c> when unknown. Must be provided together with <paramref name="YearOfBirthLatest"/>.
/// </param>
/// <param name="YearOfBirthLatest">
/// The chronologically latest year the character could have been born in (negative for BBY, positive for ABY),
/// or <c>null</c> when unknown. Must be provided together with <paramref name="YearOfBirthEarliest"/>.
/// </param>
/// <param name="YearOfDeathEarliest">
/// The chronologically earliest year the character could have died in (negative for BBY, positive for ABY), or
/// <c>null</c> when unknown. Must be provided together with <paramref name="YearOfDeathLatest"/>.
/// </param>
/// <param name="YearOfDeathLatest">
/// The chronologically latest year the character could have died in (negative for BBY, positive for ABY), or
/// <c>null</c> when unknown. Must be provided together with <paramref name="YearOfDeathEarliest"/>.
/// </param>
/// <param name="SpeciesId">The identifier of the character's species, or <c>null</c> when it is unknown.</param>
public record CreateCharacterRequest(
    string Name,
    Guid? PlanetBornOnId = null,
    int? YearOfBirthEarliest = null,
    int? YearOfBirthLatest = null,
    int? YearOfDeathEarliest = null,
    int? YearOfDeathLatest = null,
    Guid? SpeciesId = null);
