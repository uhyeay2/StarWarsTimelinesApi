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

    private static UserSourceMaterial Item(Guid userId, SourceMaterial source, TrackingStatus status) =>
        new()
        {
            UserId = userId,
            SourceMaterialId = source.Id,
            Status = status,
            IsFavorite = false,
            SourceMaterial = source
        };

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
    public async Task GetLibraryAsync_IncludesUnitsWithProgress()
    {
        var source = Source();
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
        var source = SourceWithUnits(3);
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
        var source = SourceWithUnits(3);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.WishListed);
        _repository
            .Setup(x => x.GetLibraryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterial> { item });
        var units = source.SourceMaterialUnits.ToList();
        _progress
            .Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSourceMaterialUnit>
            {
                new() { UserId = userId, SourceMaterialUnitId = units[0].Id, IsCompleted = true }
            });

        var result = await _service.GetLibraryAsync(userId);

        var single = Assert.Single(result);
        Assert.Equal(TrackingStatus.InProgress, single.Status);
    }

    [Fact]
    public async Task GetLibraryAsync_WithAllUnitsCompleted_DerivesCompleted()
    {
        var source = SourceWithUnits(3);
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
    public async Task UpdateAsync_ForUnitBasedMaterial_WhenStatusProvided_ThrowsAndNeverSaves()
    {
        var source = SourceWithUnits(1);
        var userId = Guid.NewGuid();
        var item = Item(userId, source, TrackingStatus.InProgress);
        _repository
            .Setup(x => x.GetByIdAsync(userId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateAsync(userId, source.Id, new UpdateLibraryItemRequest(TrackingStatus.Completed, null)));

        Assert.Equal("Status", exception.ParamName);
        _repository.Verify(x => x.Update(It.IsAny<UserSourceMaterial>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
}
