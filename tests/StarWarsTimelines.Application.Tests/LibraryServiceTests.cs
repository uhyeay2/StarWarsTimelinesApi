using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class LibraryServiceTests
{
    private readonly Mock<IUserSourceMaterialRepository> _repository;
    private readonly Mock<ISourceMaterialRepository> _catalog;
    private readonly Mock<ISourceMaterialUnitRepository> _units;
    private readonly Mock<IUserSourceMaterialUnitRepository> _progress;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly LibraryService _service;

    public LibraryServiceTests()
    {
        _repository = new Mock<IUserSourceMaterialRepository>();
        _catalog = new Mock<ISourceMaterialRepository>();
        _units = new Mock<ISourceMaterialUnitRepository>();
        _progress = new Mock<IUserSourceMaterialUnitRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new LibraryService(_repository.Object, _catalog.Object, _units.Object, _progress.Object, _unitOfWork.Object);

        _progress
            .Setup(x => x.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserSourceMaterialUnit>());
    }

    private static SourceMaterial Source() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "A New Hope",
            Medium = Medium.Movie,
            CanonType = CanonType.CanonAndLegends
        };

    private static SourceMaterial SourceWithUnits(int count)
    {
        var source = Source();
        source.SourceMaterialUnits = Enumerable.Range(1, count)
            .Select(number => new SourceMaterialUnit
            {
                Id = Guid.NewGuid(),
                SourceMaterialId = source.Id,
                UnitType = UnitType.Episode,
                Number = number
            })
            .ToList();
        return source;
    }

    private static SourceMaterial SourceWithUnitsAndMedium(int count, Medium medium, UnitType unitType)
    {
        var source = Source();
        source.Medium = medium;
        source.SourceMaterialUnits = Enumerable.Range(1, count)
            .Select(number => new SourceMaterialUnit
            {
                Id = Guid.NewGuid(),
                SourceMaterialId = source.Id,
                UnitType = unitType,
                Number = number
            })
            .ToList();
        return source;
    }

    private static UserSourceMaterial Item(Guid userId, SourceMaterial source, TrackingStatus status) =>
        new()
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = status,
            IsFavorite = false,
            SourceMaterial = source
        };

    private static SourceMaterial ShowWithSeasons(params int[] episodeCountsPerSeason)
    {
        var source = Source();
        source.Medium = Medium.AnimatedShow;
        var units = new List<SourceMaterialUnit>();
        for (var season = 1; season <= episodeCountsPerSeason.Length; season++)
        {
            units.Add(new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Season, Number = season });
            for (var episode = 1; episode <= episodeCountsPerSeason[season - 1]; episode++)
            {
                units.Add(new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Episode, GroupNumber = season, Number = episode });
            }
        }

        source.SourceMaterialUnits = units;
        return source;
    }

    private List<UserSourceMaterialUnit> ProgressFor(Guid userId, SourceMaterial source, params Guid[] completedUnitIds)
    {
        var completed = completedUnitIds.ToHashSet();
        return source.SourceMaterialUnits
            .Select(u => new UserSourceMaterialUnit { UserId = userId, SourceMaterialUnitId = u.Id, IsCompleted = completed.Contains(u.Id) })
            .ToList();
    }

    [Fact]
    public async Task GetLibraryAsync_ReturnsMappedItems()
    {
        var source = Source();
        var item = new UserSourceMaterial
        {
            UserId = Guid.NewGuid(),
            SourceMaterialId = source.Id,
            Status = TrackingStatus.Completed,
            IsFavorite = true,
            SourceMaterial = source
        };
        _repository
            .Setup(x => x.GetLibraryAsync(item.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });

        var result = await _service.GetLibraryAsync(item.UserId);

        var single = Assert.Single(result);
        Assert.Equal(item.SourceMaterialId, single.SourceMaterialId);
        Assert.Equal("A New Hope", single.Title);
        Assert.Equal(TrackingStatus.Completed, single.Status);
        Assert.True(single.IsFavorite);
    }

    [Fact]
    public async Task AddAsync_WhenSourceMissing_ReturnsNullAndNeverSaves()
    {
        _catalog
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterial?)null);

        var result = await _service.AddAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenAlreadyTracked_ReturnsExistingWithoutDuplicating()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var existing = new UserSourceMaterial
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = TrackingStatus.InProgress,
            IsFavorite = false,
            SourceMaterial = source
        };
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.AddAsync(userId, source.Id);

        Assert.Equal(existing.SourceMaterialId, result!.SourceMaterialId);
        Assert.Equal(TrackingStatus.InProgress, result.Status);
        _repository.Verify(x => x.AddAsync(It.IsAny<UserSourceMaterial>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_AddsNewItemWithWishListedStatus()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var saved = new UserSourceMaterial
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = TrackingStatus.WishListed,
            IsFavorite = false,
            SourceMaterial = source
        };
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .SetupSequence(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null)
            .ReturnsAsync(saved);

        var result = await _service.AddAsync(userId, source.Id);

        Assert.Equal(TrackingStatus.WishListed, result!.Status);
        Assert.False(result.IsFavorite);
        _repository.Verify(
            x => x.AddAsync(It.Is<UserSourceMaterial>(i => i.Status == TrackingStatus.WishListed && !i.IsFavorite), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_AssignsNextSortOrder()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var saved = new UserSourceMaterial
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = TrackingStatus.WishListed,
            IsFavorite = false,
            SourceMaterial = source
        };
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .SetupSequence(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null)
            .ReturnsAsync(saved);
        _repository
            .Setup(x => x.GetNextSortOrderAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _service.AddAsync(userId, source.Id);

        Assert.NotNull(result);
        _repository.Verify(
            x => x.AddAsync(It.Is<UserSourceMaterial>(i => i.SortOrder == 5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReorderAsync_ReordersItemsAndReturnsLibrary()
    {
        var userId = Guid.NewGuid();
        var sourceOne = Source();
        var sourceTwo = Source();
        var items = new List<UserSourceMaterial>
        {
            new() { UserId = userId, SourceMaterialId = sourceOne.Id, Status = TrackingStatus.WishListed, IsFavorite = false, SourceMaterial = sourceOne, SortOrder = 0 },
            new() { UserId = userId, SourceMaterialId = sourceTwo.Id, Status = TrackingStatus.WishListed, IsFavorite = false, SourceMaterial = sourceTwo, SortOrder = 1 }
        };
        _repository
            .Setup(x => x.GetTrackedItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => items.OrderBy(x => x.SortOrder).ToList());

        var result = await _service.ReorderAsync(userId, new List<Guid> { sourceTwo.Id, sourceOne.Id });

        Assert.Equal(2, result.Count);
        Assert.Equal(sourceTwo.Id, result[0].SourceMaterialId);
        Assert.Equal(sourceOne.Id, result[1].SourceMaterialId);
        Assert.Equal(1, items.Single(x => x.SourceMaterialId == sourceOne.Id).SortOrder);
        Assert.Equal(0, items.Single(x => x.SourceMaterialId == sourceTwo.Id).SortOrder);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Exactly(2));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReorderAsync_WhenListHasExtraItem_ThrowsAndNeverSaves()
    {
        var userId = Guid.NewGuid();
        var sourceOne = Source();
        var items = new List<UserSourceMaterial>
        {
            new() { UserId = userId, SourceMaterialId = sourceOne.Id, Status = TrackingStatus.WishListed, IsFavorite = false, SourceMaterial = sourceOne, SortOrder = 0 }
        };
        _repository
            .Setup(x => x.GetTrackedItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReorderAsync(userId, new List<Guid> { sourceOne.Id, Guid.NewGuid() }));

        Assert.Equal("orderedSourceMaterialIds", exception.ParamName);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReorderAsync_WhenListMissingItem_ThrowsAndNeverSaves()
    {
        var userId = Guid.NewGuid();
        var sourceOne = Source();
        var sourceTwo = Source();
        var items = new List<UserSourceMaterial>
        {
            new() { UserId = userId, SourceMaterialId = sourceOne.Id, Status = TrackingStatus.WishListed, IsFavorite = false, SourceMaterial = sourceOne, SortOrder = 0 },
            new() { UserId = userId, SourceMaterialId = sourceTwo.Id, Status = TrackingStatus.WishListed, IsFavorite = false, SourceMaterial = sourceTwo, SortOrder = 1 }
        };
        _repository
            .Setup(x => x.GetTrackedItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReorderAsync(userId, new List<Guid> { sourceOne.Id }));

        Assert.Equal("orderedSourceMaterialIds", exception.ParamName);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReorderAsync_WhenListHasDuplicate_ThrowsAndNeverSaves()
    {
        var userId = Guid.NewGuid();
        var sourceOne = Source();
        var items = new List<UserSourceMaterial>
        {
            new() { UserId = userId, SourceMaterialId = sourceOne.Id, Status = TrackingStatus.WishListed, IsFavorite = false, SourceMaterial = sourceOne, SortOrder = 0 }
        };
        _repository
            .Setup(x => x.GetTrackedItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReorderAsync(userId, new List<Guid> { sourceOne.Id, sourceOne.Id }));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangesStatusAndFavorite()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var item = new UserSourceMaterial
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = TrackingStatus.WishListed,
            IsFavorite = false,
            SourceMaterial = source
        };
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var updated = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.Completed, true));

        Assert.NotNull(updated);
        Assert.Equal(TrackingStatus.Completed, updated.Status);
        Assert.True(updated.IsFavorite);
        _repository.Verify(x => x.Update(It.Is<UserSourceMaterial>(i => i.Status == TrackingStatus.Completed && i.IsFavorite)), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateLibraryItemRequest(null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_RemovesItem()
    {
        var item = new UserSourceMaterial
        {
            UserId = Guid.NewGuid(),
            SourceMaterialId = Guid.NewGuid(),
            Status = TrackingStatus.WishListed,
            IsFavorite = false,
            SourceMaterial = Source()
        };
        _repository
            .Setup(x => x.GetByIdAsync(item.UserId, item.SourceMaterialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var removed = await _service.RemoveAsync(item.UserId, item.SourceMaterialId);

        Assert.True(removed);
        _repository.Verify(x => x.Remove(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenMissing_ReturnsFalse()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null);

        var removed = await _service.RemoveAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedItemWithProgress()
    {
        var source = Source();
        source.SourceMaterialUnits =
        [
            new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Episode, Number = 1 },
            new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Episode, Number = 2 }
        ];
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterialUnit>
            {
                new() { UserId = userId, SourceMaterialUnitId = source.SourceMaterialUnits.First().Id, IsCompleted = true }
            });

        var result = await _service.GetByIdAsync(userId, source.Id);

        Assert.NotNull(result);
        Assert.Equal(source.Id, result!.SourceMaterialId);
        Assert.Equal("A New Hope", result.Title);
        Assert.Equal(TrackingStatus.InProgress, result.Status);
        Assert.True(result.Units[0].IsCompleted);
        Assert.True(result.Units[0].IsTracked);
        Assert.False(result.Units[1].IsCompleted);
        Assert.False(result.Units[1].IsTracked);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotTracked_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLibraryAsync_IncludesUnitsWithProgress()
    {        var source = Source();
        source.SourceMaterialUnits =
        [
            new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Episode, Number = 1 },
            new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Episode, Number = 2 },
            new SourceMaterialUnit { Id = Guid.NewGuid(), SourceMaterialId = source.Id, UnitType = UnitType.Episode, Number = 3 }
        ];
        var item = new UserSourceMaterial
        {
            UserId = Guid.NewGuid(),
            SourceMaterialId = source.Id,
            Status = TrackingStatus.InProgress,
            IsFavorite = false,
            SourceMaterial = source
        };
        _repository
            .Setup(x => x.GetLibraryAsync(item.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });
        var progressUnits = source.SourceMaterialUnits.ToList();
        _progress
            .Setup(x => x.GetByUserAsync(item.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterialUnit>
            {
                new() { UserId = item.UserId, SourceMaterialUnitId = progressUnits[1].Id, IsCompleted = true }
            });

        var result = await _service.GetLibraryAsync(item.UserId);

        var single = Assert.Single(result);
        Assert.Equal(3, single.Units.Count);
        Assert.Equal(new[] { 1, 2, 3 }, single.Units.Select(u => u.Number));
        Assert.False(single.Units[0].IsCompleted);
        Assert.True(single.Units[1].IsCompleted);
        Assert.False(single.Units[2].IsCompleted);
    }

    [Fact]
    public async Task GetLibraryAsync_WithNoUnitsCompleted_DerivesWishListed()
    {
        var source = ShowWithSeasons(1, 1);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.WishListed, single.Status);
    }

    [Fact]
    public async Task GetLibraryAsync_WithSomeUnitsCompleted_DerivesInProgress()
    {
        var source = ShowWithSeasons(1, 1);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });
        var firstEpisode = source.SourceMaterialUnits.First(u => u.UnitType == UnitType.Episode);
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterialUnit>
            {
                new() { UserId = userId, SourceMaterialUnitId = firstEpisode.Id, IsCompleted = true }
            });

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.InProgress, single.Status);
    }

    [Fact]
    public async Task GetLibraryAsync_WithAllUnitsCompleted_DerivesCompleted()
    {
        var source = ShowWithSeasons(1, 1);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source.SourceMaterialUnits
                .Select(u => new UserSourceMaterialUnit
                {
                    UserId = userId,
                    SourceMaterialUnitId = u.Id,
                    IsCompleted = true
                })
                .ToList());

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.Completed, single.Status);
    }

    [Fact]
    public async Task GetLibraryAsync_WithoutUnits_KeepsManualStatus()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.WishListed, single.Status);
    }

    [Fact]
    public async Task GetLibraryAsync_ForBookWithChapters_KeepsManualStatusEvenWhenChaptersCompleted()
    {
        var source = SourceWithUnitsAndMedium(3, Medium.Book, UnitType.Chapter);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source.SourceMaterialUnits
                .Select(u => new UserSourceMaterialUnit
                {
                    UserId = userId,
                    SourceMaterialUnitId = u.Id,
                    IsCompleted = true
                })
                .ToList());

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.InProgress, single.Status);
    }

    [Fact]
    public async Task UpdateAsync_ForSeasonBasedMaterial_WhenStatusProvided_WithoutUnitId_ThrowsAndNeverSaves()
    {
        var source = ShowWithSeasons(2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.Completed, null)));

        Assert.Equal("UnitId", exception.ParamName);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ForBookWithoutUnitId_SetsMaterialLevelStatus()
    {
        var source = SourceWithUnitsAndMedium(3, Medium.Book, UnitType.Chapter);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.InProgress, null));

        Assert.NotNull(result);
        Assert.Equal(TrackingStatus.InProgress, result!.Status);
        Assert.Equal(TrackingStatus.InProgress, item.Status);
        _repository.Verify(x => x.Update(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ForUnitBasedMaterial_FavoriteOnly_SucceedsAndKeepsDerivedStatus()
    {
        var source = SourceWithUnits(1);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(null, true));

        Assert.NotNull(result);
        Assert.True(result!.IsFavorite);
        Assert.Equal(TrackingStatus.WishListed, result.Status);
        Assert.True(item.IsFavorite);
        Assert.Equal(TrackingStatus.WishListed, item.Status);
        _repository.Verify(x => x.Update(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetUnitProgressAsync_WhenUnitMissing_ReturnsNullAndNeverSaves()
    {
        _units
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialUnit?)null);

        var result = await _service.SetUnitProgressAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true);

        Assert.Null(result);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetUnitProgressAsync_WhenUnitNotInMaterial_ReturnsNull()
    {
        var unit = new SourceMaterialUnit
        {
            Id = Guid.NewGuid(),
            SourceMaterialId = Guid.NewGuid(),
            UnitType = UnitType.Episode,
            Number = 1
        };
        _units
            .Setup(x => x.GetByIdAsync(unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unit);

        var result = await _service.SetUnitProgressAsync(Guid.NewGuid(), Guid.NewGuid(), unit.Id, true);

        Assert.Null(result);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetUnitProgressAsync_WhenItemNotTracked_ReturnsNull()
    {
        var source = Source();
        var unit = new SourceMaterialUnit
        {
            Id = Guid.NewGuid(),
            SourceMaterialId = source.Id,
            UnitType = UnitType.Episode,
            Number = 1
        };
        _units
            .Setup(x => x.GetByIdAsync(unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unit);
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null);

        var result = await _service.SetUnitProgressAsync(Guid.NewGuid(), source.Id, unit.Id, true);

        Assert.Null(result);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetUnitProgressAsync_CreatesProgressWhenMissing()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var unit = new SourceMaterialUnit
        {
            Id = Guid.NewGuid(),
            SourceMaterialId = source.Id,
            UnitType = UnitType.Episode,
            Number = 1
        };
        var item = new UserSourceMaterial
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = TrackingStatus.InProgress,
            IsFavorite = false,
            SourceMaterial = source
        };
        _units
            .Setup(x => x.GetByIdAsync(unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unit);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _progress
            .Setup(x => x.GetByIdAsync(userId, unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterialUnit?)null);

        var result = await _service.SetUnitProgressAsync(userId, source.Id, unit.Id, true);

        Assert.NotNull(result);
        Assert.Equal(unit.Id, result!.Id);
        Assert.True(result.IsCompleted);
        _progress.Verify(
            x => x.AddAsync(It.Is<UserSourceMaterialUnit>(p => p.UserId == userId && p.SourceMaterialUnitId == unit.Id && p.IsCompleted), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetUnitProgressAsync_UpdatesExistingProgress()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var unit = new SourceMaterialUnit
        {
            Id = Guid.NewGuid(),
            SourceMaterialId = source.Id,
            UnitType = UnitType.Episode,
            Number = 1
        };
        var item = new UserSourceMaterial
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = TrackingStatus.InProgress,
            IsFavorite = false,
            SourceMaterial = source
        };
        var record = new UserSourceMaterialUnit
        {
            UserId = userId,
            SourceMaterialUnitId = unit.Id,
            IsCompleted = false
        };
        _units
            .Setup(x => x.GetByIdAsync(unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unit);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _progress
            .Setup(x => x.GetByIdAsync(userId, unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _service.SetUnitProgressAsync(userId, source.Id, unit.Id, true);

        Assert.NotNull(result);
        Assert.True(result!.IsCompleted);
        Assert.True(record.IsCompleted);
        _progress.Verify(x => x.Update(record), Times.Once);
        _progress.Verify(x => x.AddAsync(It.IsAny<UserSourceMaterialUnit>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithStatus_UsesProvidedStatus()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var addedItem = Item(userId, source, TrackingStatus.InProgress);
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .SetupSequence(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null)
            .ReturnsAsync(addedItem);
        _repository
            .Setup(x => x.GetNextSortOrderAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.AddAsync(userId, source.Id, TrackingStatus.InProgress);

        Assert.NotNull(result);
        Assert.Equal(TrackingStatus.InProgress, result!.Status);
        _repository.Verify(
            x => x.AddAsync(It.Is<UserSourceMaterial>(u => u.Status == TrackingStatus.InProgress), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithoutStatus_DefaultsToWishListed()
    {
        var source = Source();
        var userId = Guid.NewGuid();
        var addedItem = Item(userId, source, TrackingStatus.WishListed);
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .SetupSequence(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null)
            .ReturnsAsync(addedItem);
        _repository
            .Setup(x => x.GetNextSortOrderAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _service.AddAsync(userId, source.Id);

        Assert.NotNull(result);
        Assert.Equal(TrackingStatus.WishListed, result!.Status);
        _repository.Verify(
            x => x.AddAsync(It.Is<UserSourceMaterial>(u => u.Status == TrackingStatus.WishListed), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ForShow_WithUnitId_SetsSingleUnitAndSaves()
    {
        var source = SourceWithUnitsAndMedium(3, Medium.AnimatedShow, UnitType.Episode);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.Completed, null, source.SourceMaterialUnits.ElementAt(1).Id));

        Assert.NotNull(result);
        _repository.Verify(x => x.Update(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ForBook_WithUnitId_SetsAllUnitsAndSaves()
    {
        var source = SourceWithUnitsAndMedium(3, Medium.Book, UnitType.Chapter);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.Completed, null, source.SourceMaterialUnits.ElementAt(0).Id));

        Assert.NotNull(result);
        _repository.Verify(x => x.Update(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ForUnitBasedMaterial_WithInvalidUnitId_Throws()
    {
        var source = SourceWithUnits(2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var invalidUnitId = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.Completed, null, invalidUnitId)));

        Assert.Equal("UnitId", exception.ParamName);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ForSeason_RecordsSeasonProgressAndCascadesToItsEpisodesOnly()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var seasonTwo = source.SourceMaterialUnits.Single(u => u.UnitType == UnitType.Season && u.Number == 2);

        var result = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.InProgress, null, seasonTwo.Id));

        Assert.NotNull(result);
        _progress.Verify(
            x => x.AddAsync(It.Is<UserSourceMaterialUnit>(p => p.SourceMaterialUnitId == seasonTwo.Id && !p.IsCompleted), It.IsAny<CancellationToken>()),
            Times.Once);
        foreach (var episode in source.SourceMaterialUnits.Where(u => u.UnitType == UnitType.Episode && u.GroupNumber == 2))
        {
            _progress.Verify(
                x => x.AddAsync(It.Is<UserSourceMaterialUnit>(p => p.SourceMaterialUnitId == episode.Id && !p.IsCompleted), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // One row for the season plus one per episode in that season; other seasons are untouched.
        _progress.Verify(x => x.AddAsync(It.IsAny<UserSourceMaterialUnit>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ForSeason_WishListed_RemovesSeasonAndEpisodeProgress()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProgressFor(userId, source));
        var seasonOne = source.SourceMaterialUnits.Single(u => u.UnitType == UnitType.Season && u.Number == 1);
        var expectedRemovedIds = new[] { seasonOne.Id }
            .Concat(source.SourceMaterialUnits.Where(u => u.UnitType == UnitType.Episode && u.GroupNumber == 1).Select(u => u.Id))
            .OrderBy(id => id)
            .ToList();

        var result = await _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.WishListed, null, seasonOne.Id));

        Assert.NotNull(result);
        _progress.Verify(
            x => x.RemoveRange(It.Is<IEnumerable<UserSourceMaterialUnit>>(list =>
                list.Select(r => r.SourceMaterialUnitId).OrderBy(id => id).SequenceEqual(expectedRemovedIds))),
            Times.Once);
        _progress.Verify(x => x.AddAsync(It.IsAny<UserSourceMaterialUnit>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearUnitProgressAsync_RemovesSeasonAndEpisodeProgressButKeepsItem()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProgressFor(userId, source));
        var seasonTwo = source.SourceMaterialUnits.Single(u => u.UnitType == UnitType.Season && u.Number == 2);
        var expectedRemovedIds = new[] { seasonTwo.Id }
            .Concat(source.SourceMaterialUnits.Where(u => u.UnitType == UnitType.Episode && u.GroupNumber == 2).Select(u => u.Id))
            .OrderBy(id => id)
            .ToList();

        var result = await _service.ClearUnitProgressAsync(userId, source.Id, seasonTwo.Id);

        Assert.True(result);
        _progress.Verify(
            x => x.RemoveRange(It.Is<IEnumerable<UserSourceMaterialUnit>>(list =>
                list.Select(r => r.SourceMaterialUnitId).OrderBy(id => id).SequenceEqual(expectedRemovedIds))),
            Times.Once);
        _repository.Verify(x => x.Update(item), Times.Once);
        _repository.Verify(x => x.Remove(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearUnitProgressAsync_RemovesLibraryEntry_WhenLastProgressCleared()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var seasonTwo = source.SourceMaterialUnits.Single(u => u.UnitType == UnitType.Season && u.Number == 2);
        var seasonTwoWithEpisodes = new HashSet<Guid> { seasonTwo.Id }
            .Concat(source.SourceMaterialUnits.Where(u => u.UnitType == UnitType.Episode && u.GroupNumber == 2).Select(u => u.Id))
            .ToHashSet();
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProgressFor(userId, source).Where(r => seasonTwoWithEpisodes.Contains(r.SourceMaterialUnitId)).ToList());

        var result = await _service.ClearUnitProgressAsync(userId, source.Id, seasonTwo.Id);

        Assert.True(result);
        _repository.Verify(x => x.Remove(item), Times.Once);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearUnitProgressAsync_WhenNoExistingProgress_KeepsItemWithoutSaving()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var seasonOne = source.SourceMaterialUnits.Single(u => u.UnitType == UnitType.Season && u.Number == 1);

        var result = await _service.ClearUnitProgressAsync(userId, source.Id, seasonOne.Id);

        Assert.True(result);
        _repository.Verify(x => x.Remove(It.IsAny<UserSourceMaterial>()), Times.Never);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearUnitProgressAsync_WhenItemMissing_ReturnsFalse()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSourceMaterial?)null);

        var result = await _service.ClearUnitProgressAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ClearUnitProgressAsync_WhenUnitNotInMaterial_ThrowsAndNeverSaves()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ClearUnitProgressAsync(userId, source.Id, Guid.NewGuid()));

        Assert.Equal("unitId", exception.ParamName);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLibraryAsync_WithExplicitIncompleteSeasonProgress_DerivesInProgress()
    {
        var source = ShowWithSeasons(2, 2);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });
        var seasonOne = source.SourceMaterialUnits.Single(u => u.UnitType == UnitType.Season && u.Number == 1);
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterialUnit>
            {
                new() { UserId = userId, SourceMaterialUnitId = seasonOne.Id, IsCompleted = false }
            });

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.InProgress, single.Status);
        var mappedSeasonOne = single.Units.Single(u => u.Id == seasonOne.Id);
        Assert.False(mappedSeasonOne.IsCompleted);
        Assert.True(mappedSeasonOne.IsTracked);
        var untouchedEpisode = single.Units.First(u => u.UnitType == Domain.Enums.UnitType.Episode && u.GroupNumber == 2);
        Assert.False(untouchedEpisode.IsCompleted);
        Assert.False(untouchedEpisode.IsTracked);
    }
}
