namespace StarWarsTimelines.Application.Dtos;

/// <summary>
/// Represents the payload used to reorder a user's library.
/// </summary>
/// <param name="OrderedSourceMaterialIds">
/// The complete desired order of the user's library items. Every item must appear exactly once.
/// </param>
public record ReorderLibraryItemsRequest(IReadOnlyList<Guid> OrderedSourceMaterialIds);
