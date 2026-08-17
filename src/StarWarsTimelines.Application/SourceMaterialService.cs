using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages the source material catalog.
/// </summary>
public sealed class SourceMaterialService : ISourceMaterialService
{
    private readonly ISourceMaterialRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="SourceMaterialService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist source materials.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public SourceMaterialService(ISourceMaterialRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceMaterialResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(SourceMaterialResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<SourceMaterialResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : SourceMaterialResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<SourceMaterialResponse> CreateAsync(CreateSourceMaterialRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);

        var item = new SourceMaterial
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Medium = request.Medium ?? Medium.Movie,
            CanonType = request.CanonType ?? CanonType.Canon
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SourceMaterialResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<SourceMaterialResponse?> UpdateAsync(Guid id, UpdateSourceMaterialRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (request.Title is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
            item.Title = request.Title.Trim();
        }

        if (request.Medium is Medium medium)
        {
            item.Medium = medium;
        }

        if (request.CanonType is CanonType canonType)
        {
            item.CanonType = canonType;
        }

        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SourceMaterialResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        if (await _repository.IsReferencedAsync(id, cancellationToken))
        {
            throw new ConflictException($"Source material '{item.Title}' is referenced by timeline events or user libraries and cannot be deleted.");
        }

        _repository.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
