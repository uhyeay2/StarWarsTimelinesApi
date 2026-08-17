using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Tests;

public sealed class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly LocationService _service;

    public LocationServiceTests()
    {
        _repository = new Mock<ILocationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new LocationService(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedItems()
    {
        var item = new Location { Id = Guid.NewGuid(), Name = "Tython" };
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Location> { item });

        var result = await _service.GetAllAsync();

        var single = Assert.Single(result);
        Assert.Equal(item.Id, single.Id);
        Assert.Equal("Tython", single.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem()
    {
        var item = new Location { Id = Guid.NewGuid(), Name = "Coruscant" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("Coruscant", result.Name);
    }

    [Fact]
    public async Task CreateAsync_AddsItemAndReturnsResponse()
    {
        var result = await _service.CreateAsync(new CreateLocationRequest("  Naboo  "));

        Assert.Equal("Naboo", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);

        _repository.Verify(
            x => x.AddAsync(It.Is<Location>(i => i.Name == "Naboo"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidName_Throws(string? name)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(new CreateLocationRequest(name!)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangesName()
    {
        var item = new Location { Id = Guid.NewGuid(), Name = "Old name" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(item.Id, new UpdateLocationRequest("New name"));

        Assert.NotNull(result);
        Assert.Equal("New name", result.Name);
        _repository.Verify(x => x.Update(It.Is<Location>(i => i.Name == "New name")), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidName_Throws()
    {
        var item = new Location { Id = Guid.NewGuid(), Name = "Keep me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.UpdateAsync(item.Id, new UpdateLocationRequest("   ")));

        _repository.Verify(x => x.Update(It.IsAny<Location>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateLocationRequest(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = new Location { Id = Guid.NewGuid(), Name = "Delete me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var removed = await _service.DeleteAsync(item.Id);

        Assert.True(removed);
        _repository.Verify(x => x.Remove(item), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var removed = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task DeleteAsync_WhenReferencedByEvent_ThrowsConflict()
    {
        var item = new Location { Id = Guid.NewGuid(), Name = "Linked location" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repository
            .Setup(x => x.IsReferencedByEventAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(item.Id));

        _repository.Verify(x => x.Remove(It.IsAny<Location>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
