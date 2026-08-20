using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to add a source material to a user's library.
/// </summary>
/// <param name="SourceMaterialId">The identifier of the source material to track.</param>
/// <param name="Status">The initial tracking status, or <c>null</c> to default to <see cref="TrackingStatus.WishListed"/>.</param>
public record AddLibraryItemRequest(Guid SourceMaterialId, TrackingStatus? Status = null);
