using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to partially update a library item. Omitted (null) fields are left unchanged.
/// </summary>
/// <param name="Status">The new progress status, or <c>null</c> to leave it unchanged.</param>
/// <param name="IsFavorite">The new favorite flag, or <c>null</c> to leave it unchanged.</param>
public record UpdateLibraryItemRequest(TrackingStatus? Status, bool? IsFavorite);
