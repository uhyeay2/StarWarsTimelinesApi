using Moq;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application.Tests;

public sealed class SourceMaterialServiceTests
{
    private readonly Mock<ISourceMaterialRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly SourceMaterialService _service;

    public SourceMaterialServiceTests()
    {
        _repository = new Mock<ISourceMaterialRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new SourceMaterialService(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_AddsItemAndReturnsResponse()
    {
        var request = new CreateSourceMaterialRequest("A New Hope", Medium.Movie, CanonType.CanonAndLegends);

        var result = await _service.CreateAsync(request);

        Assert.Equal("A New Hope", result.Title);
        Assert.Equal(Medium.Movie, result.Medium);
        Assert.Equal(CanonType.CanonAndLegends, result.CanonType);
        Assert.NotEqual(Guid.Empty, result.Id);

        _repository.Verify(
            x => x.AddAsync(It.Is<SourceMaterial>(i => i.Title == "A New Hope"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DefaultsMediumAndCanonType()
    {
        var result = await _service.CreateAsync(new CreateSourceMaterialRequest("Test", null, null));

        Assert.Equal(Medium.Movie, result.Medium);
        Assert.Equal(CanonType.Canon, result.CanonType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithInvalidTitle_Throws(string? title)
    {
        var request = new CreateSourceMaterialRequest(title!, null, null);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateAsync(request));

        _repository.Verify(x => x.AddAsync(It.IsAny<SourceMaterial>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterial?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem()
    {
        var item = new SourceMaterial
        {
            Id = Guid.NewGuid(),
            Title = "Darth Bane: Path of Destruction",
            Medium = Medium.Book,
            CanonType = CanonType.Legends
        };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Id);
        Assert.Equal("Darth Bane: Path of Destruction", result.Title);
        Assert.Equal(CanonType.Legends, result.CanonType);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        _repository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceMaterial>
            {
                new() { Id = Guid.NewGuid(), Title = "One", Medium = Medium.Movie, CanonType = CanonType.Canon },
                new() { Id = Guid.NewGuid(), Title = "Two", Medium = Medium.Book, CanonType = CanonType.Legends }
            });

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ChangesTitleMediumAndCanonType()
    {
        var item = new SourceMaterial
        {
            Id = Guid.NewGuid(),
            Title = "Old title",
            Medium = Medium.Movie,
            CanonType = CanonType.Canon
        };
        _repository
            .Setup(x => x.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var updated = await _service.UpdateAsync(
            item.Id,
            new UpdateSourceMaterialRequest("New title", Medium.Book, CanonType.CanonAndLegends));

        Assert.NotNull(updated);
        Assert.Equal("New title", updated.Title);
        Assert.Equal(Medium.Book, updated.Medium);
        Assert.Equal(CanonType.CanonAndLegends, updated.CanonType);

        _repository.Verify(x => x.Update(It.Is<SourceMaterial>(i => i.Title == "New title")), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        _repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SourceMaterial?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateSourceMaterialRequest(null, null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = new SourceMaterial
        {
            Id = Guid.NewGuid(),
            Title = "Delete me",
            Medium = Medium.Movie,
            CanonType = CanonType.Canon
        };
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
            .ReturnsAsync((SourceMaterial?)null);

        var deleted = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
