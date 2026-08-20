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

            // If updating a season unit, cascade the status to all episodes in that season
            if (unit.GroupNumber.HasValue)
            {
                // This is a season unit - update all episodes in that season
                var seasonNumber = unit.GroupNumber.Value;
                var episodesInSeason = item.SourceMaterial.SourceMaterialUnits
                    .Where(u => u.GroupNumber == seasonNumber && u.UnitType == UnitType.Episode)
                    .ToList();

                foreach (var episode in episodesInSeason)
                {
                    await SetUnitProgressCoreAsync(userId, episode.Id, isCompleted, cancellationToken);
                }
            }
            else
            {
                // This is an episode unit - update just that episode
                await SetUnitProgressCoreAsync(userId, request.UnitId.Value, isCompleted, cancellationToken);
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

        return LibraryUnitResponse.FromEntity(unit, isCompleted);
    }

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

        // Use the actual UnitType of the first unit to determine derivation hierarchy
        var unitType = units.FirstOrDefault()?.UnitType ?? UnitType.Episode;

        return LibraryItemResponse.FromEntity(
            item,
            DeriveStatus(item, units, progress, unitType),
            units.Select(u => LibraryUnitResponse.FromEntity(u, progress.GetValueOrDefault(u.Id))).ToList());
    }

    /// <summary>
    /// Derives the effective tracking status of a library item from its unit progress. When the material has no
    /// sub-units the manually tracked status is used. Otherwise the status reflects the share of completed units:
    /// none completed means <see cref="TrackingStatus.WishListed"/>, all completed means
    /// <see cref="TrackingStatus.Completed"/>, and anything in between means <see cref="TrackingStatus.InProgress"/>.
    /// </summary>
    /// <param name="item">The library entity whose stored status is used as a fallback for materials without units.</param>
    /// <param name="units">The material's sub-units (could be episodes, seasons, or a mix).</param>
    /// <param name="progress">The user's unit progress keyed by unit identifier.</param>
    /// <param name="unitType">The type of the top-level units (used to determine derivation hierarchy).</param>
    /// <returns>The derived <see cref="TrackingStatus"/>.</returns>
    private static TrackingStatus DeriveStatus(
        UserSourceMaterial item,
        IReadOnlyCollection<SourceMaterialUnit> units,
        IReadOnlyDictionary<Guid, bool> progress,
        UnitType? unitType = null)
    {
        if (units.Count == 0)
        {
            return item.Status;
        }

        // Group units by their group number (season number)
        var groupedBySeason = units
            .Where(u => u.GroupNumber.HasValue)
            .GroupBy(u => u.GroupNumber!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // If we have season-level units, derive season status first
        if (unitType.HasValue && unitType.Value == UnitType.Season && groupedBySeason.Count > 0)
        {
            // Derive status for each season from its episodes
            var seasonCompletedCounts = new Dictionary<int, int>();
            foreach (var seasonUnits in groupedBySeason.Values)
            {
                var seasonCompleted = seasonUnits.Count(u => progress.GetValueOrDefault(u.Id));
                seasonCompletedCounts[seasonUnits[0].GroupNumber!.Value] = seasonCompleted;
            }

            // Show status is derived from seasons:
            // All completed → Completed, None completed → WishListed, else InProgress
            var allSeasons = groupedBySeason.Keys.ToList();
            var totalCompleted = seasonCompletedCounts.Values.Sum();
            var totalSeasons = seasonCompletedCounts.Count;

            if (totalCompleted == totalSeasons)
            {
                return TrackingStatus.Completed;
            }
            if (totalCompleted == 0)
            {
                return TrackingStatus.WishListed;
            }
            return TrackingStatus.InProgress;
        }

        // Original derivation for episode-level units (no grouping or non-season units)
        var completed = units.Count(u => progress.GetValueOrDefault(u.Id));
        if (completed == 0) return TrackingStatus.WishListed;
        if (completed == units.Count) return TrackingStatus.Completed;
        return TrackingStatus.InProgress;
    }
}
