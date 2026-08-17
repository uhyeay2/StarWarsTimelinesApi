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
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="CharacterService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist characters.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public CharacterService(ICharacterRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var item = new Character
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CharacterResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<CharacterResponse?> UpdateAsync(Guid id, UpdateCharacterRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
            item.Name = request.Name.Trim();
        }

        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CharacterResponse.FromEntity(item);
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
}
