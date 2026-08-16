using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages timeline events and their links to characters, locations, and vehicles.
/// </summary>
public sealed class SourceMaterialEventService : ISourceMaterialEventService
{
    private readonly ISourceMaterialEventRepository _repository;
    private readonly ISourceMaterialRepository _catalog;
    private readonly ICharacterRepository _characters;
    private readonly ILocationRepository _locations;
    private readonly IVehicleRepository _vehicles;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="SourceMaterialEventService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist timeline events.</param>
    /// <param name="catalog">The repository used to validate events' source materials against the catalog.</param>
    /// <param name="characters">The repository used to validate event character links.</param>
    /// <param name="locations">The repository used to validate event location links.</param>
    /// <param name="vehicles">The repository used to validate event vehicle links.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public SourceMaterialEventService(
        ISourceMaterialEventRepository repository,
        ISourceMaterialRepository catalog,
        ICharacterRepository characters,
        ILocationRepository locations,
        IVehicleRepository vehicles,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _catalog = catalog;
        _characters = characters;
        _locations = locations;
        _vehicles = vehicles;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceMaterialEventResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(SourceMaterialEventResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<SourceMaterialEventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : SourceMaterialEventResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<SourceMaterialEventResponse> CreateAsync(CreateSourceMaterialEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        await ValidateReferencesAsync(request.SourceMaterialId, request.CharacterIds, request.LocationIds, request.VehicleIds, cancellationToken);

        var item = new SourceMaterialEvent
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            CanonType = request.CanonType,
            Year = request.Year,
            DisplayDate = request.DisplayDate,
            DisplayDateEnd = request.DisplayDateEnd,
            SourceMaterialId = request.SourceMaterialId,
            EventCharacters = request.CharacterIds.Select(x => new EventCharacter { CharacterId = x }).ToList(),
            EventLocations = request.LocationIds.Select(x => new EventLocation { LocationId = x }).ToList(),
            EventVehicles = request.VehicleIds.Select(x => new EventVehicle { VehicleId = x }).ToList()
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetByIdAsync(item.Id, cancellationToken);
        return SourceMaterialEventResponse.FromEntity(created!);
    }

    /// <inheritdoc />
    public async Task<SourceMaterialEventResponse?> UpdateAsync(Guid id, UpdateSourceMaterialEventRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdTrackedAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (request.Title is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
            item.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            item.Description = request.Description;
        }

        if (request.CanonType is Domain.Enums.CanonType canonType)
        {
            item.CanonType = canonType;
        }

        if (request.Year is int year)
        {
            item.Year = year;
        }

        if (request.DisplayDate is not null)
        {
            item.DisplayDate = request.DisplayDate;
        }

        item.DisplayDateEnd = request.DisplayDateEnd ?? item.DisplayDateEnd;

        if (request.SourceMaterialId is Guid sourceMaterialId)
        {
            if (await _catalog.GetByIdAsync(sourceMaterialId, cancellationToken) is null)
            {
                throw new ArgumentException($"Source material '{sourceMaterialId}' does not exist.", nameof(request));
            }

            item.SourceMaterialId = sourceMaterialId;
        }

        if (request.CharacterIds is not null)
        {
            await ValidateCharactersAsync(request.CharacterIds, cancellationToken);
            ReplaceLinks(item.EventCharacters, request.CharacterIds, static id => new EventCharacter { CharacterId = id });
        }

        if (request.LocationIds is not null)
        {
            await ValidateLocationsAsync(request.LocationIds, cancellationToken);
            ReplaceLinks(item.EventLocations, request.LocationIds, static id => new EventLocation { LocationId = id });
        }

        if (request.VehicleIds is not null)
        {
            await ValidateVehiclesAsync(request.VehicleIds, cancellationToken);
            ReplaceLinks(item.EventVehicles, request.VehicleIds, static id => new EventVehicle { VehicleId = id });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetByIdAsync(item.Id, cancellationToken);
        return updated is null ? null : SourceMaterialEventResponse.FromEntity(updated);
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
    /// Verifies that the referenced source material and every linked character, location, and vehicle exist.
    /// </summary>
    /// <param name="sourceMaterialId">The source material identifier to validate.</param>
    /// <param name="characterIds">The character identifiers to validate.</param>
    /// <param name="locationIds">The location identifiers to validate.</param>
    /// <param name="vehicleIds">The vehicle identifiers to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the source material or any referenced character, location, or vehicle does not exist.
    /// </exception>
    private async Task ValidateReferencesAsync(
        Guid sourceMaterialId,
        IReadOnlyList<Guid> characterIds,
        IReadOnlyList<Guid> locationIds,
        IReadOnlyList<Guid> vehicleIds,
        CancellationToken cancellationToken)
    {
        if (await _catalog.GetByIdAsync(sourceMaterialId, cancellationToken) is null)
        {
            throw new ArgumentException($"Source material '{sourceMaterialId}' does not exist.", nameof(sourceMaterialId));
        }

        await ValidateCharactersAsync(characterIds, cancellationToken);
        await ValidateLocationsAsync(locationIds, cancellationToken);
        await ValidateVehiclesAsync(vehicleIds, cancellationToken);
    }

    /// <summary>
    /// Verifies that every referenced character exists.
    /// </summary>
    /// <param name="characterIds">The character identifiers to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when a referenced character does not exist.</exception>
    private async Task ValidateCharactersAsync(IReadOnlyList<Guid> characterIds, CancellationToken cancellationToken)
    {
        foreach (var id in characterIds)
        {
            if (await _characters.GetByIdAsync(id, cancellationToken) is null)
            {
                throw new ArgumentException($"Character '{id}' does not exist.", nameof(characterIds));
            }
        }
    }

    /// <summary>
    /// Verifies that every referenced location exists.
    /// </summary>
    /// <param name="locationIds">The location identifiers to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when a referenced location does not exist.</exception>
    private async Task ValidateLocationsAsync(IReadOnlyList<Guid> locationIds, CancellationToken cancellationToken)
    {
        foreach (var id in locationIds)
        {
            if (await _locations.GetByIdAsync(id, cancellationToken) is null)
            {
                throw new ArgumentException($"Location '{id}' does not exist.", nameof(locationIds));
            }
        }
    }

    /// <summary>
    /// Verifies that every referenced vehicle exists.
    /// </summary>
    /// <param name="vehicleIds">The vehicle identifiers to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when a referenced vehicle does not exist.</exception>
    private async Task ValidateVehiclesAsync(IReadOnlyList<Guid> vehicleIds, CancellationToken cancellationToken)
    {
        foreach (var id in vehicleIds)
        {
            if (await _vehicles.GetByIdAsync(id, cancellationToken) is null)
            {
                throw new ArgumentException($"Vehicle '{id}' does not exist.", nameof(vehicleIds));
            }
        }
    }

    /// <summary>
    /// Replaces the links of a tracked event collection with new link entities for the given identifiers, letting
    /// the change tracker delete removed links and insert added ones.
    /// </summary>
    /// <typeparam name="TLink">The link entity type in the collection.</typeparam>
    /// <param name="collection">The tracked link collection to replace.</param>
    /// <param name="ids">The identifiers the collection should link to.</param>
    /// <param name="factory">A factory that builds a link entity for an identifier.</param>
    private static void ReplaceLinks<TLink>(ICollection<TLink> collection, IReadOnlyList<Guid> ids, Func<Guid, TLink> factory)
    {
        collection.Clear();
        foreach (var id in ids)
        {
            collection.Add(factory(id));
        }
    }
}
