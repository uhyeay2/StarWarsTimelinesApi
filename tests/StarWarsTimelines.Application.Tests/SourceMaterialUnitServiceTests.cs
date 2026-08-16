using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class SourceMaterialUnitServiceTests
{
    private readonly Mock<ISourceMaterialUnitRepository> _repository;
    private readonly Mock<ISourceMaterialRepository> _catalog;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly SourceMaterialUnitService _service;

    public SourceMaterialUnitServiceTests()
    {
        _repository = new Mock<ISourceMaterialUnitRepository>();
        _catalog = new Mock<ISourceMaterialRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new SourceMaterialUnitService(_repository.Object, _catalog.Object, _unitOfWork.Object);
    }

    private static SourceMaterial Source() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "The Mandalorian",
            Medium = Medium.LiveActionShow,
            CanonType = CanonType.Canon
        };

    private static SourceMaterialUnit Unit(Guid sourceMaterialId, int number) =>
        new()
        {
            Id = Guid.NewGuid(),
            SourceMaterialId = sourceMaterialId,
            UnitType = UnitType.Episode,
            Number = number,
            Title = $"Chapter {number}"
        };

    [Fact]
    public async Task GetBySourceMaterialAsync_WhenMaterialMissing_ReturnsNull()
    {
        _catalog
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterial?)null);

        var result = await _service.GetBySourceMaterialAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySourceMaterialAsync_ReturnsUnitsOrderedByNumber()
    {
        var source = Source();
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .Setup(x => x.GetBySourceMaterialAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceMaterialUnit> { Unit(source.Id, 1), Unit(source.Id, 2), Unit(source.Id, 3) });

        var result = await _service.GetBySourceMaterialAsync(source.Id);

        Assert.NotNull(result);
        Assert.Equal(new[] { 1, 2, 3 }, result!.Select(u => u.Number));
        Assert.All(result, u => Assert.Equal(source.Id, u.SourceMaterialId));
    }

    [Fact]
    public async Task CreateAsync_WhenMaterialMissing_ReturnsNull()
    {
        _catalog
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterial?)null);

        var result = await _service.CreateAsync(Guid.NewGuid(), new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, null));

        Assert.Null(result);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithBlankTitle_NormalizesToNull()
    {
        var source = Source();
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .Setup(x => x.GetByNumberAsync(source.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialUnit?)null);

        var result = await _service.CreateAsync(source.Id, new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, "   "));

        Assert.NotNull(result);
        Assert.Null(result!.Title);
        _repository.Verify(
            x => x.AddAsync(It.Is<SourceMaterialUnit>(u => u.SourceMaterialId == source.Id && u.Number == 1 && u.Title == null), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNumber_Throws()
    {
        var source = Source();
        var existing = Unit(source.Id, 1);
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .Setup(x => x.GetByNumberAsync(source.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(source.Id, new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, null)));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNumberLessThanOne_Throws()
    {
        var source = Source();
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(source.Id, new CreateSourceMaterialUnitRequest(UnitType.Episode, 0, null)));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CreatesUnit()
    {
        var source = Source();
        _catalog
            .Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _repository
            .Setup(x => x.GetByNumberAsync(source.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialUnit?)null);

        var result = await _service.CreateAsync(source.Id, new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, "Chapter 1"));

        Assert.NotNull(result);
        Assert.Equal(source.Id, result!.SourceMaterialId);
        Assert.Equal(UnitType.Episode, result.UnitType);
        Assert.Equal(1, result.Number);
        Assert.Equal("Chapter 1", result.Title);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialUnit?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateSourceMaterialUnitRequest(null, null, null));

        Assert.Null(result);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFields()
    {
        var source = Source();
        var item = Unit(source.Id, 1);
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repository
            .Setup(x => x.GetByNumberAsync(source.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialUnit?)null);

        var result = await _service.UpdateAsync(item.Id, new UpdateSourceMaterialUnitRequest(UnitType.Chapter, 2, "Renamed"));

        Assert.NotNull(result);
        Assert.Equal(UnitType.Chapter, result!.UnitType);
        Assert.Equal(2, result.Number);
        Assert.Equal("Renamed", result.Title);
        _repository.Verify(x => x.Update(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnNumber_IsAllowed()
    {
        var source = Source();
        var item = Unit(source.Id, 1);
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repository
            .Setup(x => x.GetByNumberAsync(source.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(item.Id, new UpdateSourceMaterialUnitRequest(null, 1, "Title"));

        Assert.NotNull(result);
        Assert.Equal(1, result!.Number);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateNumberOnAnotherUnit_Throws()
    {
        var source = Source();
        var item = Unit(source.Id, 1);
        var other = Unit(source.Id, 2);
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repository
            .Setup(x => x.GetByNumberAsync(source.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateAsync(item.Id, new UpdateSourceMaterialUnitRequest(null, 2, null)));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUnit()
    {
        var source = Source();
        var item = Unit(source.Id, 1);
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var deleted = await _service.DeleteAsync(item.Id);

        Assert.True(deleted);
        _repository.Verify(x => x.Remove(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialUnit?)null);

        var deleted = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
