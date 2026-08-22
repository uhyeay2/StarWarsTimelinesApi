using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application.Tests;

public sealed class CharacterServiceTests
{
    private readonly Mock<ICharacterRepository> _repository;
    private readonly Mock<ILocationRepository> _locations;
    private readonly Mock<ISpeciesRepository> _species;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly CharacterService _service;

    public CharacterServiceTests()
    {
        _repository = new Mock<ICharacterRepository>();
        _locations = new Mock<ILocationRepository>();
        _species = new Mock<ISpeciesRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new CharacterService(_repository.Object, _locations.Object, _species.Object, _unitOfWork.Object);

        // CreateAsync re-reads the character after saving so the response carries navigation data.
        Character? added = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Character>(), It.IsAny<CancellationToken>()))
            .Callback<Character, CancellationToken>((item, _) => added = item)
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedItems()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Luke Skywalker" };
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Character> { item });

        var result = await _service.GetAllAsync();

        var single = Assert.Single(result);
        Assert.Equal(item.Id, single.Id);
        Assert.Equal("Luke Skywalker", single.Name);
    }

    [Fact]
    public async Task GetAllAsync_IncludesBiographicalAttributes()
    {
        var planetId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();
        var item = new Character
        {
            Id = Guid.NewGuid(),
            Name = "Padme Amidala",
            PlanetBornOnId = planetId,
            PlanetBornOn = new Location { Id = planetId, Name = "Naboo" },
            YearOfBirthEarliest = -46,
            YearOfBirthLatest = -46,
            YearOfDeathEarliest = -19,
            YearOfDeathLatest = -19,
            SpeciesId = speciesId,
            Species = new Species { Id = speciesId, Name = "Human" }
        };
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Character> { item });

        var result = await _service.GetAllAsync();

        var single = Assert.Single(result);
        Assert.Equal("Naboo", single.PlanetBornOnName);
        Assert.Equal(-46, single.YearOfBirthEarliest);
        Assert.Equal(-19, single.YearOfDeathLatest);
        Assert.Equal("Human", single.SpeciesName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Character?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Grogu" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal("Grogu", result.Name);
    }

    [Fact]
    public async Task CreateAsync_AddsItemAndReturnsResponse()
    {
        var result = await _service.CreateAsync(new CreateCharacterRequest("  Ahsoka Tano  "));

        Assert.Equal("Ahsoka Tano", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);

        _repository.Verify(
            x => x.AddAsync(It.Is<Character>(i => i.Name == "Ahsoka Tano"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithBiography_PopulatesAllAttributes()
    {
        var planetId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync(new Location());
        _species.Setup(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>())).ReturnsAsync(new Species());

        // Palpatine: born on Naboo between 88 and 84 BBY, first died in 4 ABY.
        var result = await _service.CreateAsync(new CreateCharacterRequest(
            "Emperor Palpatine", planetId, -88, -84, 4, 35, speciesId));

        _repository.Verify(
            x => x.AddAsync(It.Is<Character>(i =>
                i.PlanetBornOnId == planetId &&
                i.YearOfBirthEarliest == -88 &&
                i.YearOfBirthLatest == -84 &&
                i.YearOfDeathEarliest == 4 &&
                i.YearOfDeathLatest == 35 &&
                i.SpeciesId == speciesId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(planetId, result.PlanetBornOnId);
        Assert.Equal(speciesId, result.SpeciesId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidName_Throws(string? name)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(new CreateCharacterRequest(name!)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownBirthPlanet_Throws()
    {
        var planetId = Guid.NewGuid();
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync((Location?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.CreateAsync(new CreateCharacterRequest("Luke Skywalker", planetId)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownSpecies_Throws()
    {
        var speciesId = Guid.NewGuid();
        _species.Setup(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>())).ReturnsAsync((Species?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.CreateAsync(new CreateCharacterRequest("Ahsoka Tano", null, null, null, null, null, speciesId)));

        _repository.Verify(x => x.AddAsync(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvertedBirthRange_Throws()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.CreateAsync(new CreateCharacterRequest("Emperor Palpatine", null, -84, -88)));
    }

    [Fact]
    public async Task CreateAsync_WithInvertedDeathRange_Throws()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.CreateAsync(new CreateCharacterRequest("Emperor Palpatine", null, null, null, 35, 4)));
    }

    [Fact]
    public async Task UpdateAsync_ChangesName()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Old name" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.UpdateAsync(item.Id, new UpdateCharacterRequest("New name"));

        Assert.NotNull(result);
        Assert.Equal("New name", result.Name);
        _repository.Verify(x => x.Update(It.Is<Character>(i => i.Name == "New name")), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithYearPair_SetsBothValues()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Yoda" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Yoda's age is only known approximately: between 890 and 900 BBY. Note that -900 precedes -890.
        var result = await _service.UpdateAsync(item.Id, new UpdateCharacterRequest(YearOfBirthEarliest: -900, YearOfBirthLatest: -890));

        Assert.NotNull(result);
        Assert.Equal(-900, result.YearOfBirthEarliest);
        Assert.Equal(-890, result.YearOfBirthLatest);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithHalfAYearRange_Throws()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Keep me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.UpdateAsync(item.Id, new UpdateCharacterRequest(YearOfBirthEarliest: -36)));

        _repository.Verify(x => x.Update(It.IsAny<Character>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvertedYearRange_Throws()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Keep me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.UpdateAsync(item.Id, new UpdateCharacterRequest(YearOfDeathEarliest: 35, YearOfDeathLatest: 4)));

        _repository.Verify(x => x.Update(It.IsAny<Character>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownBirthPlanet_Throws()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Keep me" };
        var planetId = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync((Location?)null);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.UpdateAsync(item.Id, new UpdateCharacterRequest(PlanetBornOnId: planetId)));

        _repository.Verify(x => x.Update(It.IsAny<Character>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidBiographyFields_AppliesThem()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Revan" };
        var planetId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _locations.Setup(x => x.GetByIdAsync(planetId, It.IsAny<CancellationToken>())).ReturnsAsync(new Location());
        _species.Setup(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>())).ReturnsAsync(new Species());

        var result = await _service.UpdateAsync(item.Id, new UpdateCharacterRequest(PlanetBornOnId: planetId, SpeciesId: speciesId));

        Assert.NotNull(result);
        Assert.Equal(planetId, result.PlanetBornOnId);
        Assert.Equal(speciesId, result.SpeciesId);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidName_Throws()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Keep me" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.UpdateAsync(item.Id, new UpdateCharacterRequest("   ")));

        _repository.Verify(x => x.Update(It.IsAny<Character>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Character?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateCharacterRequest(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Delete me" };
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
            .ReturnsAsync((Character?)null);

        var removed = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task DeleteAsync_WhenReferencedByEvent_ThrowsConflict()
    {
        var item = new Character { Id = Guid.NewGuid(), Name = "Linked character" };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _repository
            .Setup(x => x.IsReferencedByEventAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(item.Id));

        _repository.Verify(x => x.Remove(It.IsAny<Character>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
