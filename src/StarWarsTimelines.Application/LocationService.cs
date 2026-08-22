using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages the location catalog.
/// </summary>
public sealed class LocationService : ILocationService
{
    private readonly ILocationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="LocationService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist locations.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public LocationService(ILocationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocationResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(LocationResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<LocationResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : LocationResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default)
    {
        RequireName(request.Name);

        var item = new Location
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LocationResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<LocationResponse?> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        RequireName(request.Name);
        item.Name = request.Name.Trim();

        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return LocationResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        if (await _repository.IsReferencedByEventAsync(id, cancellationToken))
        {
            throw new ConflictException($"Location '{item.Name}' is linked to one or more timeline events and cannot be deleted.");
        }

        _repository.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Ensures a location name was provided.
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
