namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to update a character. The request replaces the character's data: optional
/// attributes sent as <c>null</c> are cleared back to unknown, and the required name must be supplied on
/// every call.
/// </summary>
/// <param name="Name">The new name. Required; a blank value is rejected.</param>
/// <param name="PlanetBornOnId">The birth planet identifier, or <c>null</c> for an unknown birth planet.</param>
/// <param name="YearOfBirthEarliest">
/// The chronologically earliest birth year (negative for BBY, positive for ABY), or <c>null</c> when unknown.
/// Must be provided together with <paramref name="YearOfBirthLatest"/>.
/// </param>
/// <param name="YearOfBirthLatest">
/// The chronologically latest birth year (negative for BBY, positive for ABY), or <c>null</c> when unknown.
/// Must be provided together with <paramref name="YearOfBirthEarliest"/>.
/// </param>
/// <param name="YearOfDeathEarliest">
/// The chronologically earliest death year (negative for BBY, positive for ABY), or <c>null</c> when unknown.
/// Must be provided together with <paramref name="YearOfDeathLatest"/>.
/// </param>
/// <param name="YearOfDeathLatest">
/// The chronologically latest death year (negative for BBY, positive for ABY), or <c>null</c> when unknown.
/// Must be provided together with <paramref name="YearOfDeathEarliest"/>.
/// </param>
/// <param name="SpeciesId">The species identifier, or <c>null</c> for an unknown species.</param>
public record UpdateCharacterRequest(
    string Name,
    Guid? PlanetBornOnId,
    int? YearOfBirthEarliest,
    int? YearOfBirthLatest,
    int? YearOfDeathEarliest,
    int? YearOfDeathLatest,
    Guid? SpeciesId);
