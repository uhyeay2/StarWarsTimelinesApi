namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update a character. Properties left <c>null</c> are unchanged; there
/// is currently no way to clear an optional attribute back to unknown once set.
/// </summary>
/// <param name="Name">The new name, or <c>null</c> to leave it unchanged.</param>
/// <param name="PlanetBornOnId">
/// The new birth planet identifier, or <c>null</c> to leave it unchanged.
/// </param>
/// <param name="YearOfBirthEarliest">
/// The new chronologically earliest birth year (negative for BBY, positive for ABY), or <c>null</c> to leave it
/// unchanged. Must be provided together with <paramref name="YearOfBirthLatest"/>.
/// </param>
/// <param name="YearOfBirthLatest">
/// The new chronologically latest birth year (negative for BBY, positive for ABY), or <c>null</c> to leave it
/// unchanged. Must be provided together with <paramref name="YearOfBirthEarliest"/>.
/// </param>
/// <param name="YearOfDeathEarliest">
/// The new chronologically earliest death year (negative for BBY, positive for ABY), or <c>null</c> to leave it
/// unchanged. Must be provided together with <paramref name="YearOfDeathLatest"/>.
/// </param>
/// <param name="YearOfDeathLatest">
/// The new chronologically latest death year (negative for BBY, positive for ABY), or <c>null</c> to leave it
/// unchanged. Must be provided together with <paramref name="YearOfDeathEarliest"/>.
/// </param>
/// <param name="SpeciesId">The new species identifier, or <c>null</c> to leave it unchanged.</param>
public record UpdateCharacterRequest(
    string? Name = null,
    Guid? PlanetBornOnId = null,
    int? YearOfBirthEarliest = null,
    int? YearOfBirthLatest = null,
    int? YearOfDeathEarliest = null,
    int? YearOfDeathLatest = null,
    Guid? SpeciesId = null);
