using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Tests;

public sealed class SpeciesServiceTests
{
    private readonly Mock<ISpeciesRepository> _repository;
    private readonly Mock<ILocationRepository> _locations;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly SpeciesService _service;

    public SpeciesServiceTests()
    {
        _repository = new Mock<ISpeciesRepository>();
        _locations = new Mock<ILocationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new SpeciesService(_repository.Object, _locations.Object, _unitOfWork.Object);

        // CreateAsync re-reads the species after saving so the response carries navigation data.
        Species? added = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Species>(), It.IsAny<CancellationToken>()))
            .Callback<Species, CancellationToken>((item, _) => added = item)
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedItems()
    {
        var planetId = Guid.NewGuid();
        var item = new Species
        {
            Id = Guid.NewGuid(),
            Name = "Twi'lek",
            HomePlanetId = planetId,
            HomePlanet = new Location { Id = planetId, Name = "Ryloth" }
        };
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Species> { item });

        var result = await _service.GetAllAsync();

        var single = Assert.Single(result);
        Assert.Equal("Twi'lek", single.Name);
        Assert.Equal(planetId, single.HomePlanetId);
        Assert.Equal("Ryloth", single.HomePlanetName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Species?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithoutHomePlanet_AddsItemAndReturnsResponse()
    {
        var result = await _service.CreateAsync(new CreateSpeciesRequest("  Yoda's species  "));

        Assert.Equal("Yoda's species", result.Name);
        Assert.Null(result.HomePlanetId);
        Assert.Null(result.HomePlanetName);

        _repository.Verify(
            x => x.AddAsync(It.Is<Species>(i => i.Name == "Yoda's species" && i.HomePlanetId == null), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithKnownHomePlanet_SetsTheReference()
    {
        var planetId = Guid.NewGuid();
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync(new Location { Id = planetId, Name = "Kashyyyk" });

        var result = await _service.CreateAsync(new CreateSpeciesRequest("Wookiee", planetId));

        _repository.Verify(
            x => x.AddAsync(It.Is<Species>(i => i.HomePlanetId == planetId), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(planetId, result.HomePlanetId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidName_Throws(string? name)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(new CreateSpeciesRequest(name!)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Species>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownHomePlanet_Throws()
    {
        var planetId = Guid.NewGuid();
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync((Location?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.CreateAsync(new CreateSpeciesRequest("Mirialan", planetId)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Species>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesData()
    {
        var item = new Species { Id = Guid.NewGuid(), Name = "Old name", HomePlanetId = Guid.NewGuid() };
        var planetId = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync(new Location { Id = planetId, Name = "Iridonia" });

        var result = await _service.UpdateAsync(item.Id, new UpdateSpeciesRequest("New name", planetId));

        Assert.NotNull(result);
        Assert.Equal("New name", result.Name);
        Assert.Equal(planetId, result.HomePlanetId);
        _repository.Verify(x => x.Update(It.Is<Species>(i => i.Name == "New name" && i.HomePlanetId == planetId)), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullHomePlanet_ClearsHomePlanet()
    {
        var planetId = Guid.NewGuid();
        var item = new Species { Id = Guid.NewGuid(), Name = "Zabrak", HomePlanetId = planetId };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(item.Id, new UpdateSpeciesRequest("Zabrak", null));

        Assert.NotNull(result);
        Assert.Null(result.HomePlanetId);
    }

    [Fact]
    public async Task UpdateAsync_SetsHomePlanet()
    {
        var item = new Species { Id = Guid.NewGuid(), Name = "Zabrak" };
        var planetId = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync(new Location { Id = planetId, Name = "Iridonia" });

        var result = await _service.UpdateAsync(item.Id, new UpdateSpeciesRequest("Zabrak", planetId));

        Assert.NotNull(result);
        Assert.Equal(planetId, result.HomePlanetId);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownHomePlanet_Throws()
    {
        var item = new Species { Id = Guid.NewGuid(), Name = "Keep me" };
        var planetId = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync((Location?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.UpdateAsync(item.Id, new UpdateSpeciesRequest("Mirialan", planetId)));

        _repository.Verify(x => x.Update(It.IsAny<Species>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidName_Throws()
    {
        var item = new Species { Id = Guid.NewGuid(), Name = "Keep me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.UpdateAsync(item.Id, new UpdateSpeciesRequest("   ", null)));

        _repository.Verify(x => x.Update(It.IsAny<Species>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Species?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateSpeciesRequest("New name", null));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = new Species { Id = Guid.NewGuid(), Name = "Delete me" };
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
            .ReturnsAsync((Species?)null);

        var removed = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(removed);
    }
}
