namespace StarWarsTimelines.Domain.Entities;

/// <summary>
/// Represents a named starship, walker, or other vehicle from the Star Wars universe that can be linked to
/// timeline events.
/// </summary>
/// <remarks>
/// Vehicles form an admin-managed lookup catalog. They are never queried with their associated events; the
/// linking is owned by the <see cref="EventVehicle"/> table, so this entity has no inverse collection.
/// </remarks>
public sealed class Vehicle
{
    /// <summary>
    /// Gets or sets the unique identifier of the vehicle.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the vehicle's name (for example, "Millennium Falcon").
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
