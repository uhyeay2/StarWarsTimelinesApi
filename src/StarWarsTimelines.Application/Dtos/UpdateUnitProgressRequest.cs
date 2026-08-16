namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to set a user's progress on a single source material unit.
/// </summary>
/// <param name="IsCompleted">A value indicating whether the user has completed the unit.</param>
public record UpdateUnitProgressRequest(bool IsCompleted);
