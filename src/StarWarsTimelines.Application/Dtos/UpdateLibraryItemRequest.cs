using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update a library item. Omitted (null) fields are left unchanged.
/// </summary>
/// <param name="Status">The new progress status, or <c>null</c> to leave it unchanged.</param>
/// <param name="IsFavorite">The new favorite flag, or <c>null</c> to leave it unchanged.</param>
/// <param name="UnitId">
/// The identifier of a specific unit to update when setting status on a unit-based material. When provided for a
/// <see cref="Medium.Book"/>, all units are marked consistently with the status. For other media with sub-units, only
/// the specified unit's progress is updated and the material status is derived from unit progress.
/// </param>
public record UpdateLibraryItemRequest(TrackingStatus? Status, bool? IsFavorite, Guid? UnitId = null);
