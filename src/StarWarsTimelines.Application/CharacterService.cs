using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages the character catalog.
/// </summary>
public sealed class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _repository;
    private readonly ILocationRepository _locations;
    private readonly ISpeciesRepository _species;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="CharacterService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist characters.</param>
    /// <param name="locations">The repository used to validate characters' birth planets against the catalog.</param>
    /// <param name="species">The repository used to validate characters' species against the catalog.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public CharacterService(ICharacterRepository repository, ILocationRepository locations, ISpeciesRepository species, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _locations = locations;
        _species = species;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CharacterResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(CharacterResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<CharacterResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : CharacterResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<CharacterResponse> CreateAsync(CreateCharacterRequest request, CancellationToken cancellationToken = default)
    {
        RequireName(request.Name);
        ValidateYearRanges(
            request.YearOfBirthEarliest,
            request.YearOfBirthLatest,
            request.YearOfDeathEarliest,
            request.YearOfDeathLatest);
        await ValidateReferencesAsync(
            request.PlanetBornOnId,
            request.SpeciesId,
            cancellationToken);

        var item = new Character
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            PlanetBornOnId = request.PlanetBornOnId,
            YearOfBirthEarliest = request.YearOfBirthEarliest,
            YearOfBirthLatest = request.YearOfBirthLatest,
            YearOfDeathEarliest = request.YearOfDeathEarliest,
            YearOfDeathLatest = request.YearOfDeathLatest,
            SpeciesId = request.SpeciesId
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetByIdAsync(item.Id, cancellationToken);
        return CharacterResponse.FromEntity(created!);
    }

    /// <inheritdoc />
    public async Task<CharacterResponse?> UpdateAsync(Guid id, UpdateCharacterRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        RequireName(request.Name);

        if ((request.YearOfBirthEarliest is int) != (request.YearOfBirthLatest is int) ||
            (request.YearOfDeathEarliest is int) != (request.YearOfDeathLatest is int))
        {
            throw new BadRequestException("Birth and death year ranges must be provided as both an earliest and a latest value.", nameof(request));
        }

        ValidateYearRanges(
            request.YearOfBirthEarliest,
            request.YearOfBirthLatest,
            request.YearOfDeathEarliest,
            request.YearOfDeathLatest);
        await ValidateReferencesAsync(
            request.PlanetBornOnId,
            request.SpeciesId,
            cancellationToken);

        item.Name = request.Name.Trim();
        item.PlanetBornOnId = request.PlanetBornOnId;
        item.YearOfBirthEarliest = request.YearOfBirthEarliest;
        item.YearOfBirthLatest = request.YearOfBirthLatest;
        item.YearOfDeathEarliest = request.YearOfDeathEarliest;
        item.YearOfDeathLatest = request.YearOfDeathLatest;
        item.SpeciesId = request.SpeciesId;

        // The tracked read loads the PlanetBornOn/Species navigations; clearing them keeps EF Core from
        // restoring the old foreign keys from still-populated references when the identifiers are nulled.
        item.PlanetBornOn = null;
        item.Species = null;

        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetByIdAsync(item.Id, cancellationToken);
        return updated is null ? null : CharacterResponse.FromEntity(updated);
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
            throw new ConflictException($"Character '{item.Name}' is linked to one or more timeline events and cannot be deleted.");
        }

        _repository.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Verifies that every referenced birth planet and species exists.
    /// </summary>
    /// <param name="planetBornOnId">The location identifier to validate, or <c>null</c> for an unknown planet.</param>
    /// <param name="speciesId">The species identifier to validate, or <c>null</c> for an unknown species.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown when a referenced location or species does not exist.</exception>
    private async Task ValidateReferencesAsync(Guid? planetBornOnId, Guid? speciesId, CancellationToken cancellationToken)
    {
        if (planetBornOnId is Guid planetId && await _locations.GetByIdAsync(planetId, cancellationToken) is null)
        {
            throw new EntityNotFoundException($"Location '{planetId}' does not exist.", nameof(planetBornOnId));
        }

        if (speciesId is Guid id && await _species.GetByIdAsync(id, cancellationToken) is null)
        {
            throw new EntityNotFoundException($"Species '{id}' does not exist.", nameof(speciesId));
        }
    }

    /// <summary>
    /// Verifies that provided birth and death year ranges are chronologically valid.
    /// </summary>
    /// <param name="birthEarliest">The earliest birth year, or <c>null</c> when unknown.</param>
    /// <param name="birthLatest">The latest birth year, or <c>null</c> when unknown.</param>
    /// <param name="deathEarliest">The earliest death year, or <c>null</c> when unknown.</param>
    /// <param name="deathLatest">The latest death year, or <c>null</c> when unknown.</param>
    /// <exception cref="BadRequestException">
    /// Thrown when an earliest year is chronologically after its latest year.
    /// </exception>
    private static void ValidateYearRanges(int? birthEarliest, int? birthLatest, int? deathEarliest, int? deathLatest)
    {
        if (birthEarliest is int b1 && birthLatest is int b2 && b1 > b2)
        {
            throw new BadRequestException("The earliest birth year must not be after the latest birth year.", nameof(birthEarliest));
        }

        if (deathEarliest is int d1 && deathLatest is int d2 && d1 > d2)
        {
            throw new BadRequestException("The earliest death year must not be after the latest death year.", nameof(deathEarliest));
        }
    }

    /// <summary>
    /// Ensures a character name was provided.
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
