namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload required to add a source material to a user's library.
/// </summary>
/// <param name="SourceMaterialId">The identifier of the source material to track.</param>
public record AddLibraryItemRequest(Guid SourceMaterialId);
