namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to update a vehicle. The request replaces the vehicle's data, so every
/// field is written as provided; <c>null</c> is only valid where the value itself is optional.
/// </summary>
/// <param name="Name">The new name. Required; a blank value is rejected.</param>
public record UpdateVehicleRequest(string Name);
