using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages the species catalog.
/// </summary>
public sealed class SpeciesService : ISpeciesService
{
    private readonly ISpeciesRepository _repository;
    private readonly ILocationRepository _locations;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="SpeciesService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist species.</param>
    /// <param name="locations">The repository used to validate species' home planets against the catalog.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public SpeciesService(ISpeciesRepository repository, ILocationRepository locations, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _locations = locations;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeciesResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(SpeciesResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<SpeciesResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : SpeciesResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<SpeciesResponse> CreateAsync(CreateSpeciesRequest request, CancellationToken cancellationToken = default)
    {
        RequireName(request.Name);
        await ValidateHomePlanetAsync(request.HomePlanetId, cancellationToken);

        var item = new Species
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            HomePlanetId = request.HomePlanetId
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetByIdAsync(item.Id, cancellationToken);
        return SpeciesResponse.FromEntity(created!);
    }

    /// <inheritdoc />
    public async Task<SpeciesResponse?> UpdateAsync(Guid id, UpdateSpeciesRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        RequireName(request.Name);
        await ValidateHomePlanetAsync(request.HomePlanetId, cancellationToken);

        item.Name = request.Name.Trim();
        item.HomePlanetId = request.HomePlanetId;

        // The tracked read loads the HomePlanet navigation; clearing it keeps EF Core from restoring the old
        // foreign key from the still-populated reference when the identifier is set to null.
        item.HomePlanet = null;

        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetByIdAsync(item.Id, cancellationToken);
        return updated is null ? null : SpeciesResponse.FromEntity(updated);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _repository.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Verifies that a referenced home planet exists.
    /// </summary>
    /// <param name="homePlanetId">The location identifier to validate, or <c>null</c> for an unknown planet.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown when the referenced location does not exist.</exception>
    private async Task ValidateHomePlanetAsync(Guid? homePlanetId, CancellationToken cancellationToken)
    {
        if (homePlanetId is Guid id && await _locations.GetByIdAsync(id, cancellationToken) is null)
        {
            throw new EntityNotFoundException($"Location '{id}' does not exist.", nameof(homePlanetId));
        }
    }

    /// <summary>
    /// Ensures a species name was provided.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <exception cref="BadRequestException">Thrown when the name is missing or blank.</exception>
    private static void RequireName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("A name is required.", nameof(name));
        }
    }
}
