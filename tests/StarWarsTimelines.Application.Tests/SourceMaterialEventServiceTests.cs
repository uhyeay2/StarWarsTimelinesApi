using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class SourceMaterialEventServiceTests
{
    private readonly Mock<ISourceMaterialEventRepository> _repository;
    private readonly Mock<ISourceMaterialRepository> _catalog;
    private readonly Mock<ICharacterRepository> _characters;
    private readonly Mock<ILocationRepository> _locations;
    private readonly Mock<IVehicleRepository> _vehicles;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly SourceMaterialEventService _service;

    public SourceMaterialEventServiceTests()
    {
        _repository = new Mock<ISourceMaterialEventRepository>();
        _catalog = new Mock<ISourceMaterialRepository>();
        _characters = new Mock<ICharacterRepository>();
        _locations = new Mock<ILocationRepository>();
        _vehicles = new Mock<IVehicleRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new SourceMaterialEventService(
            _repository.Object,
            _catalog.Object,
            _characters.Object,
            _locations.Object,
            _vehicles.Object,
            _unitOfWork.Object);
    }

    private static SourceMaterial Source() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "A New Hope",
            Medium = Medium.Movie,
            CanonType = CanonType.CanonAndLegends
        };

    private static SourceMaterialEvent Event() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "The Battle of Yavin",
            Description = "A desperate trench run.",
            CanonType = CanonType.CanonAndLegends,
            Year = 0,
            DisplayDate = "0 BBY",
            SourceMaterial = Source(),
            EventCharacters = [new EventCharacter { CharacterId = Guid.NewGuid(), Character = new Character { Id = Guid.NewGuid(), Name = "Luke Skywalker" } }],
            EventLocations = [new EventLocation { LocationId = Guid.NewGuid(), Location = new Location { Id = Guid.NewGuid(), Name = "Yavin 4" } }],
            EventVehicles = [new EventVehicle { VehicleId = Guid.NewGuid(), Vehicle = new Vehicle { Id = Guid.NewGuid(), Name = "Millennium Falcon" } }]
        };

    [Fact]
    public async Task GetAllAsync_ReturnsMappedItems()
    {
        var item = Event();
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceMaterialEvent> { item });

        var result = await _service.GetAllAsync();

        var single = Assert.Single(result);
        Assert.Equal(item.Id, single.Id);
        Assert.Equal("The Battle of Yavin", single.Title);
        Assert.Single(single.Characters);
        Assert.Single(single.Locations);
        Assert.Single(single.Vehicles);
        Assert.Equal("A New Hope", single.SourceMaterial.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialEvent?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem()
    {
        var item = Event();
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal(item.Title, result.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidTitle_Throws(string? title)
    {
        var request = new CreateSourceMaterialEventRequest(title!, "desc", CanonType.Canon, 0, "0 BBY", null, Guid.NewGuid(), [], [], []);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(request));

        _repository.Verify(x => x.AddAsync(It.IsAny<SourceMaterialEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSourceMaterialMissing_Throws()
    {
        var request = new CreateSourceMaterialEventRequest("Title", "desc", CanonType.Canon, 0, "0 BBY", null, Guid.NewGuid(), [], [], []);
        _catalog
            .Setup(x => x.GetByIdAsync(request.SourceMaterialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterial?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(request));

        _repository.Verify(x => x.AddAsync(It.IsAny<SourceMaterialEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCharacterMissing_Throws()
    {
        var source = Source();
        var missingCharacterId = Guid.NewGuid();
        var request = new CreateSourceMaterialEventRequest("Title", "desc", CanonType.Canon, 0, "0 BBY", null, source.Id, [missingCharacterId], [], []);
        _catalog.Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _characters.Setup(x => x.GetByIdAsync(missingCharacterId, It.IsAny<CancellationToken>())).ReturnsAsync((Character?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(request));

        _repository.Verify(x => x.AddAsync(It.IsAny<SourceMaterialEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenLocationMissing_Throws()
    {
        var source = Source();
        var missingLocationId = Guid.NewGuid();
        var request = new CreateSourceMaterialEventRequest("Title", "desc", CanonType.Canon, 0, "0 BBY", null, source.Id, [], [missingLocationId], []);
        _catalog.Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _locations.Setup(x => x.GetByIdAsync(missingLocationId, It.IsAny<CancellationToken>())).ReturnsAsync((Location?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(request));

        _repository.Verify(x => x.AddAsync(It.IsAny<SourceMaterialEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenVehicleMissing_Throws()
    {
        var source = Source();
        var missingVehicleId = Guid.NewGuid();
        var request = new CreateSourceMaterialEventRequest("Title", "desc", CanonType.Canon, 0, "0 BBY", null, source.Id, [], [], [missingVehicleId]);
        _catalog.Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _vehicles.Setup(x => x.GetByIdAsync(missingVehicleId, It.IsAny<CancellationToken>())).ReturnsAsync((Vehicle?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(request));

        _repository.Verify(x => x.AddAsync(It.IsAny<SourceMaterialEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AddsEventAndReturnsCreated()
    {
        var source = Source();
        var characterId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var request = new CreateSourceMaterialEventRequest(
            "The Battle of Yavin",
            "A desperate trench run.",
            CanonType.CanonAndLegends,
            0,
            "0 BBY",
            null,
            source.Id,
            [characterId],
            [locationId],
            [vehicleId]);

        var created = new SourceMaterialEvent
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CanonType = request.CanonType,
            Year = request.Year,
            DisplayDate = request.DisplayDate,
            SourceMaterial = source,
            EventCharacters = [new EventCharacter { CharacterId = characterId, Character = new Character { Id = characterId, Name = "Luke Skywalker" } }],
            EventLocations = [new EventLocation { LocationId = locationId, Location = new Location { Id = locationId, Name = "Yavin 4" } }],
            EventVehicles = [new EventVehicle { VehicleId = vehicleId, Vehicle = new Vehicle { Id = vehicleId, Name = "Millennium Falcon" } }]
        };

        _catalog.Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _characters.Setup(x => x.GetByIdAsync(characterId, It.IsAny<CancellationToken>())).ReturnsAsync(new Character { Id = characterId });
        _locations.Setup(x => x.GetByIdAsync(locationId, It.IsAny<CancellationToken>())).ReturnsAsync(new Location { Id = locationId });
        _vehicles.Setup(x => x.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>())).ReturnsAsync(new Vehicle { Id = vehicleId });
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(request);

        Assert.Equal(request.Title, result.Title);
        Assert.Single(result.Characters);
        Assert.Single(result.Locations);
        Assert.Single(result.Vehicles);
        _repository.Verify(
            x => x.AddAsync(It.Is<SourceMaterialEvent>(e => e.EventCharacters.Count == 1 && e.EventLocations.Count == 1 && e.EventVehicles.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterialEvent?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateSourceMaterialEventRequest(null, null, null, null, null, null, null, null, null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ChangesScalarsAndReplacesLinks()
    {
        var source = Source();
        var existing = Event();
        var characterId = Guid.NewGuid();
        _repository.Setup(x => x.GetByIdTrackedAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository
            .Setup(x => x.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new SourceMaterialEvent
            {
                Id = existing.Id,
                Title = "New title",
                Description = "New description",
                CanonType = CanonType.Legends,
                Year = 5,
                DisplayDate = "5 ABY",
                DisplayDateEnd = "6 ABY",
                SourceMaterial = source,
                EventCharacters = [new EventCharacter { CharacterId = characterId, Character = new Character { Id = characterId, Name = "Luke Skywalker" } }],
                EventLocations = existing.EventLocations,
                EventVehicles = existing.EventVehicles
            });
        _catalog.Setup(x => x.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _characters.Setup(x => x.GetByIdAsync(characterId, It.IsAny<CancellationToken>())).ReturnsAsync(new Character { Id = characterId });

        var result = await _service.UpdateAsync(
            existing.Id,
            new UpdateSourceMaterialEventRequest("New title", "New description", CanonType.Legends, 5, "5 ABY", "6 ABY", source.Id, [characterId], null, null));

        Assert.NotNull(result);
        Assert.Equal("New title", result.Title);
        Assert.Equal(CanonType.Legends, result.CanonType);
        Assert.Equal(5, result.Year);
        Assert.Single(result.Characters);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSourceMaterialMissing_Throws()
    {
        var existing = Event();
        _repository.Setup(x => x.GetByIdTrackedAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _catalog.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SourceMaterial?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.UpdateAsync(
            existing.Id,
            new UpdateSourceMaterialEventRequest(null, null, null, null, null, null, Guid.NewGuid(), null, null, null)));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidTitle_Throws()
    {
        var existing = Event();
        _repository.Setup(x => x.GetByIdTrackedAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.UpdateAsync(
            existing.Id,
            new UpdateSourceMaterialEventRequest("   ", null, null, null, null, null, null, null, null, null)));

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = Event();
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
            .ReturnsAsync((SourceMaterialEvent?)null);

        var removed = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(removed);
    }
}
