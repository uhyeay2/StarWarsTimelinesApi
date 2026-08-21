using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages users' personal libraries of tracked source materials.
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private readonly IUserSourceMaterialRepository _repository;
    private readonly ISourceMaterialRepository _catalog;
    private readonly ISourceMaterialUnitRepository _units;
    private readonly IUserSourceMaterialUnitRepository _progress;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="LibraryService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist library items.</param>
    /// <param name="catalog">The repository used to validate tracked source materials against the catalog.</param>
    /// <param name="units">The repository used to validate unit progress against the unit catalog.</param>
    /// <param name="progress">The repository used to persist per-unit progress records.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public LibraryService(
        IUserSourceMaterialRepository repository,
        ISourceMaterialRepository catalog,
        ISourceMaterialUnitRepository units,
        IUserSourceMaterialUnitRepository progress,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _catalog = catalog;
        _units = units;
        _progress = progress;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LibraryItemResponse>> GetLibraryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetLibraryAsync(userId, cancellationToken);
        var progress = await GetProgressAsync(userId, cancellationToken);
        return items.Select(item => MapItem(item, progress)).ToList();
    }

    /// <inheritdoc />
    public async Task<LibraryItemResponse?> GetByIdAsync(Guid userId, Guid sourceMaterialId, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var progress = await GetProgressAsync(userId, cancellationToken);
        return MapItem(item, progress);
    }

    /// <inheritdoc />
    public async Task<LibraryItemResponse?> AddAsync(Guid userId, Guid sourceMaterialId, TrackingStatus? initialStatus = null, CancellationToken cancellationToken = default)
    {
        var source = await _catalog.GetByIdAsync(sourceMaterialId, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var existing = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (existing is not null)
        {
            var existingProgress = await GetProgressAsync(userId, cancellationToken);
            return MapItem(existing, existingProgress);
        }

        await _repository.AddAsync(
            new UserSourceMaterial
            {
                UserId = userId,
                SourceMaterialId = sourceMaterialId,
                Status = initialStatus ?? TrackingStatus.WishListed,
                IsFavorite = false,
                SortOrder = await _repository.GetNextSortOrderAsync(userId, cancellationToken),
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var added = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (added is null)
        {
            return null;
        }

        var addedProgress = await GetProgressAsync(userId, cancellationToken);
        return MapItem(added, addedProgress);
    }

    /// <inheritdoc />
    public async Task<LibraryItemResponse?> UpdateAsync(
        Guid userId,
        Guid sourceMaterialId,
        UpdateLibraryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (request.Status is not null && item.SourceMaterial.SourceMaterialUnits.Count > 0)
        {
            if (request.UnitId is null)
            {
                throw new ArgumentException(
                    "UnitId is required when setting status on a source material that has sub-units.",
                    nameof(request.UnitId));
            }

            var unit = item.SourceMaterial.SourceMaterialUnits.SingleOrDefault(u => u.Id == request.UnitId);
            if (unit is null)
            {
                throw new ArgumentException(
                    "The specified unit does not belong to this source material.",
                    nameof(request.UnitId));
            }

            var isCompleted = request.Status is TrackingStatus.Completed;
            var childUnitIds = GetChildUnitIds(unit, item.SourceMaterial.SourceMaterialUnits);

            if (request.Status == TrackingStatus.WishListed)
            {
                // Wish Listed means "not started": clear the unit's progress rows instead of storing
                // not-completed ones, keeping wish-listed and untouched units indistinguishable.
                await ClearUnitProgressCoreAsync(
                    userId,
                    new[] { request.UnitId.Value }.Concat(childUnitIds).ToList(),
                    cancellationToken);
            }
            else
            {
                // Record progress for the targeted unit itself so it is reported as tracked, then
                // cascade to its child units for season/volume-style containers.
                await SetUnitProgressCoreAsync(userId, request.UnitId.Value, isCompleted, cancellationToken);
                foreach (var childId in childUnitIds)
                {
                    await SetUnitProgressCoreAsync(userId, childId, isCompleted, cancellationToken);
                }
            }
        }
        else
        {
            if (request.Status is TrackingStatus status)
            {
                item.Status = status;
            }
        }

        if (request.IsFavorite is bool isFavorite)
        {
            item.IsFavorite = isFavorite;
        }

        item.UpdatedAtUtc = DateTime.UtcNow;
        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var progress = await GetProgressAsync(userId, cancellationToken);
        return MapItem(item, progress);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid userId, Guid sourceMaterialId, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _repository.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LibraryItemResponse>> ReorderAsync(
        Guid userId,
        IReadOnlyList<Guid> orderedSourceMaterialIds,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetTrackedItemsAsync(userId, cancellationToken);
        if (orderedSourceMaterialIds.Count != items.Count ||
            orderedSourceMaterialIds.Distinct().Count() != items.Count ||
            items.Any(item => !orderedSourceMaterialIds.Contains(item.SourceMaterialId)))
        {
            throw new ArgumentException(
                "The ordered list must contain exactly the user's library items, each exactly once.",
                nameof(orderedSourceMaterialIds));
        }

        for (var index = 0; index < orderedSourceMaterialIds.Count; index++)
        {
            var item = items.Single(x => x.SourceMaterialId == orderedSourceMaterialIds[index]);
            item.SortOrder = index;
            _repository.Update(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetLibraryAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LibraryUnitResponse?> SetUnitProgressAsync(
        Guid userId,
        Guid sourceMaterialId,
        Guid unitId,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var unit = await _units.GetByIdAsync(unitId, cancellationToken);
        if (unit is null || unit.SourceMaterialId != sourceMaterialId)
        {
            return null;
        }

        var item = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        await SetUnitProgressCoreAsync(userId, unitId, isCompleted, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LibraryUnitResponse.FromEntity(unit, isCompleted, isTracked: true);
    }

    /// <inheritdoc />
    public async Task<bool> ClearUnitProgressAsync(
        Guid userId,
        Guid sourceMaterialId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(userId, sourceMaterialId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        var unit = item.SourceMaterial.SourceMaterialUnits.SingleOrDefault(u => u.Id == unitId);
        if (unit is null)
        {
            throw new ArgumentException(
                "The specified unit does not belong to this source material.",
                nameof(unitId));
        }

        var targetIds = new[] { unit.Id }
            .Concat(GetChildUnitIds(unit, item.SourceMaterial.SourceMaterialUnits))
            .ToHashSet();
        var records = await _progress.GetByUserAsync(userId, cancellationToken);
        var toRemove = records.Where(r => targetIds.Contains(r.SourceMaterialUnitId)).ToList();

        if (toRemove.Count == 0)
        {
            return true;
        }

        _progress.RemoveRange(toRemove);

        if (HasRemainingProgress(item, records.Where(r => !targetIds.Contains(r.SourceMaterialUnitId)).ToList()))
        {
            item.UpdatedAtUtc = DateTime.UtcNow;
            _repository.Update(item);
        }
        else
        {
            // The cleared unit was the last tracked content for this material: drop the library entry too.
            _repository.Remove(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Determines whether any of the user's remaining progress records belong to the given library item's material.
    /// </summary>
    /// <param name="item">The library entity whose material units are checked. Its <see cref="UserSourceMaterial.SourceMaterial"/> and the material's <see cref="SourceMaterial.SourceMaterialUnits"/> must be loaded.</param>
    /// <param name="remainingRecords">The user's progress records excluding those staged for deletion.</param>
    /// <returns><c>true</c> when at least one progress record still targets one of the material's units.</returns>
    private static bool HasRemainingProgress(UserSourceMaterial item, IReadOnlyCollection<UserSourceMaterialUnit> remainingRecords) =>
        remainingRecords.Any(r => item.SourceMaterial.SourceMaterialUnits.Any(u => u.Id == r.SourceMaterialUnitId));

    /// <summary>
    /// Creates or updates a unit progress record without additional validation.
    /// Used internally by both <see cref="SetUnitProgressAsync"/> and <see cref="UpdateAsync"/>.
    /// </summary>
    /// <param name="userId">The identifier of the user whose progress is updated.</param>
    /// <param name="unitId">The identifier of the unit to update.</param>
    /// <param name="isCompleted">Whether the unit is marked as completed.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private async Task SetUnitProgressCoreAsync(Guid userId, Guid unitId, bool isCompleted, CancellationToken cancellationToken)
    {
        var record = await _progress.GetByIdAsync(userId, unitId, cancellationToken);
        if (record is null)
        {
            await _progress.AddAsync(
                new UserSourceMaterialUnit
                {
                    UserId = userId,
                    SourceMaterialUnitId = unitId,
                    IsCompleted = isCompleted,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);
        }
        else
        {
            record.IsCompleted = isCompleted;
            record.UpdatedAtUtc = DateTime.UtcNow;
            _progress.Update(record);
        }
    }

    /// <summary>
    /// Deletes the user's progress records for the given units, if any exist.
    /// Used internally by <see cref="UpdateAsync"/> (wish-listed status) and by <see cref="ClearUnitProgressAsync"/>.
    /// </summary>
    /// <param name="userId">The identifier of the user whose progress is cleared.</param>
    /// <param name="unitIds">The identifiers of the units whose progress records are removed.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private async Task ClearUnitProgressCoreAsync(Guid userId, IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken)
    {
        if (unitIds.Count == 0)
        {
            return;
        }

        var records = await _progress.GetByUserAsync(userId, cancellationToken);
        var toRemove = records.Where(r => unitIds.Contains(r.SourceMaterialUnitId)).ToList();
        if (toRemove.Count > 0)
        {
            _progress.RemoveRange(toRemove);
        }
    }

    /// <summary>
    /// Resolves the identifiers of the child units affected by a change to the given unit: a season's episodes
    /// or an issue's sibling issues in the same volume. Other unit types have no children.
    /// </summary>
    /// <param name="unit">The unit being changed.</param>
    /// <param name="allUnits">All of the material's sub-units.</param>
    /// <returns>The identifiers of the affected child units.</returns>
    private static IEnumerable<Guid> GetChildUnitIds(SourceMaterialUnit unit, IEnumerable<SourceMaterialUnit> allUnits)
    {
        if (unit.UnitType == UnitType.Season)
        {
            return allUnits
                .Where(u => u.GroupNumber == unit.Number && u.UnitType == UnitType.Episode)
                .Select(u => u.Id);
        }

        if (unit.UnitType == UnitType.Issue)
        {
            var volumeNumber = unit.GroupNumber ?? 0;
            return allUnits
                .Where(u => u.GroupNumber == volumeNumber && u.UnitType == UnitType.Issue)
                .Select(u => u.Id);
        }

        return Enumerable.Empty<Guid>();
    }

    /// <summary>
    /// Loads a user's unit progress as a lookup keyed by unit identifier.
    /// </summary>
    /// <param name="userId">The identifier of the user whose progress is loaded.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A dictionary mapping each unit identifier to the user's completion state.</returns>
    private async Task<IReadOnlyDictionary<Guid, bool>> GetProgressAsync(Guid userId, CancellationToken cancellationToken)
    {
        var records = await _progress.GetByUserAsync(userId, cancellationToken);
        return records.ToDictionary(x => x.SourceMaterialUnitId, x => x.IsCompleted);
    }

/// <summary>
    /// Maps a library entity to a response DTO, combining the material's units with the user's progress and deriving
    /// the reported status from that progress when the material has sub-units.
    /// </summary>
    /// <param name="item">The library entity to map. Its <see cref="UserSourceMaterial.SourceMaterial"/> and the
    /// material's <see cref="SourceMaterial.SourceMaterialUnits"/> must be loaded.</param>
    /// <param name="progress">The user's unit progress keyed by unit identifier.</param>
    /// <returns>A <see cref="LibraryItemResponse"/> populated from the entity.</returns>
    private static LibraryItemResponse MapItem(UserSourceMaterial item, IReadOnlyDictionary<Guid, bool> progress)
    {
        var units = item.SourceMaterial.SourceMaterialUnits.OrderBy(u => u.GroupNumber).ThenBy(u => u.Number).ToList();

        return LibraryItemResponse.FromEntity(
            item,
            DeriveStatus(item, units, progress),
            units.Select(u => LibraryUnitResponse.FromEntity(
                u,
                progress.GetValueOrDefault(u.Id),
                progress.ContainsKey(u.Id))).ToList());
    }

    /// <summary>
    /// Derives the effective tracking status of a library item from its unit progress.
    /// For shows/comics with Season/Volume units, status is derived from those level progress (or from their
    /// child episodes/issues if no explicit season/volume progress exists).
    /// Otherwise, status is derived from all episode/issue/chapter/unit progress.
    /// </summary>
    /// <param name="item">The library entity whose stored status is used as a fallback for materials without units.</param>
    /// <param name="units">The material's sub-units.</param>
    /// <param name="progress">The user's unit progress keyed by unit identifier.</param>
    /// <returns>The derived <see cref="TrackingStatus"/>.</returns>
    private static TrackingStatus DeriveStatus(
        UserSourceMaterial item,
        IReadOnlyCollection<SourceMaterialUnit> units,
        IReadOnlyDictionary<Guid, bool> progress)
    {
        if (units.Count == 0)
        {
            return item.Status;
        }

        var seasonUnits = units.Where(u => u.UnitType == UnitType.Season).ToList();
        var volumeUnits = units.Where(u => u.UnitType == UnitType.Volume).ToList();

        // For shows: derive status from Season units (or their child episodes)
        if (seasonUnits.Count > 0)
        {
            return DeriveStatusFromGroupUnits(seasonUnits, units, progress);
        }

        // For comics: derive status from Volume units (or their child issues)
        if (volumeUnits.Count > 0)
        {
            return DeriveStatusFromGroupUnits(volumeUnits, units, progress);
        }

        // Fall back to episode/issue/chapter/level derivation
        var completed = units.Count(u => progress.GetValueOrDefault(u.Id));
        if (completed == 0) return TrackingStatus.WishListed;
        if (completed == units.Count) return TrackingStatus.Completed;
        return TrackingStatus.InProgress;
    }

    /// <summary>
    /// Derives status from Season or Volume units. If the user has explicit progress on any
    /// Season/Volume unit, that is used. Otherwise (no explicit Season/Volume progress),
    /// the status is derived from child episode/issue progress.
    /// </summary>
    private static TrackingStatus DeriveStatusFromGroupUnits(
        IReadOnlyCollection<SourceMaterialUnit> groupUnits,
        IReadOnlyCollection<SourceMaterialUnit> allUnits,
        IReadOnlyDictionary<Guid, bool> progress)
    {
        var hasExplicitProgress = groupUnits.Any(u => progress.ContainsKey(u.Id));
        if (hasExplicitProgress)
        {
            // Explicit Season/Volume rows are only written for non-wish-listed statuses, so zero
            // completions still means tracking has started: report In progress rather than Wish listed.
            var completed = groupUnits.Count(u => progress.GetValueOrDefault(u.Id));
            if (completed == groupUnits.Count) return TrackingStatus.Completed;
            return TrackingStatus.InProgress;
        }

        // No explicit Season/Volume progress; derive from child episodes/issues
        var childUnitType = groupUnits.First().UnitType == UnitType.Season ? UnitType.Episode : UnitType.Issue;
        var childCompleted = allUnits.Count(u => u.UnitType == childUnitType && progress.GetValueOrDefault(u.Id));
        var childTotal = allUnits.Count(u => u.UnitType == childUnitType);

        if (childCompleted == 0) return TrackingStatus.WishListed;
        if (childCompleted == childTotal) return TrackingStatus.Completed;
        return TrackingStatus.InProgress;
    }
}
