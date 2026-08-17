using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Tests;

public sealed class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _repository = new Mock<IVehicleRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new VehicleService(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedItems()
    {
        var item = new Vehicle { Id = Guid.NewGuid(), Name = "Millennium Falcon" };
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehicle> { item });

        var result = await _service.GetAllAsync();

        var single = Assert.Single(result);
        Assert.Equal(item.Id, single.Id);
        Assert.Equal("Millennium Falcon", single.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem()
    {
        var item = new Vehicle { Id = Guid.NewGuid(), Name = "Death Star" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("Death Star", result.Name);
    }

    [Fact]
    public async Task CreateAsync_AddsItemAndReturnsResponse()
    {
        var result = await _service.CreateAsync(new CreateVehicleRequest("  Razor Crest  "));

        Assert.Equal("Razor Crest", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);

        _repository.Verify(
            x => x.AddAsync(It.Is<Vehicle>(i => i.Name == "Razor Crest"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidName_Throws(string? name)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(new CreateVehicleRequest(name!)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangesName()
    {
        var item = new Vehicle { Id = Guid.NewGuid(), Name = "Old name" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(item.Id, new UpdateVehicleRequest("New name"));

        Assert.NotNull(result);
        Assert.Equal("New name", result.Name);
        _repository.Verify(x => x.Update(It.Is<Vehicle>(i => i.Name == "New name")), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidName_Throws()
    {
        var item = new Vehicle { Id = Guid.NewGuid(), Name = "Keep me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.UpdateAsync(item.Id, new UpdateVehicleRequest("   ")));

        _repository.Verify(x => x.Update(It.IsAny<Vehicle>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateVehicleRequest(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = new Vehicle { Id = Guid.NewGuid(), Name = "Delete me" };
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
            .ReturnsAsync((Vehicle?)null);

        var removed = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task DeleteAsync_WhenReferencedByEvent_ThrowsConflict()
    {
        var item = new Vehicle { Id = Guid.NewGuid(), Name = "Linked vehicle" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repository
            .Setup(x => x.IsReferencedByEventAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(item.Id));

        _repository.Verify(x => x.Remove(It.IsAny<Vehicle>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
