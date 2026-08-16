using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages the source material unit catalog.
/// </summary>
public sealed class SourceMaterialUnitService : ISourceMaterialUnitService
{
    private readonly ISourceMaterialUnitRepository _repository;
    private readonly ISourceMaterialRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="SourceMaterialUnitService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist units.</param>
    /// <param name="catalog">The repository used to validate units' source materials against the catalog.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public SourceMaterialUnitService(
        ISourceMaterialUnitRepository repository,
        ISourceMaterialRepository catalog,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceMaterialUnitResponse>?> GetBySourceMaterialAsync(Guid sourceMaterialId, CancellationToken cancellationToken = default)
    {
        if (await _catalog.GetByIdAsync(sourceMaterialId, cancellationToken) is null)
        {
            return null;
        }

        var items = await _repository.GetBySourceMaterialAsync(sourceMaterialId, cancellationToken);
        return items.Select(SourceMaterialUnitResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<SourceMaterialUnitResponse?> CreateAsync(Guid sourceMaterialId, CreateSourceMaterialUnitRequest request, CancellationToken cancellationToken = default)
    {
        if (await _catalog.GetByIdAsync(sourceMaterialId, cancellationToken) is null)
        {
            return null;
        }

        ValidateNumber(request.Number);
        if (await _repository.GetByNumberAsync(sourceMaterialId, request.Number, cancellationToken) is not null)
        {
            throw new ArgumentException($"Unit '{request.Number}' already exists for this source material.", nameof(request.Number));
        }

        var item = new SourceMaterialUnit
        {
            Id = Guid.NewGuid(),
            SourceMaterialId = sourceMaterialId,
            UnitType = request.UnitType,
            Number = request.Number,
            Title = NormalizeTitle(request.Title),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SourceMaterialUnitResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<SourceMaterialUnitResponse?> UpdateAsync(Guid id, UpdateSourceMaterialUnitRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (request.UnitType is Domain.Enums.UnitType unitType)
        {
            item.UnitType = unitType;
        }

        if (request.Number is int number)
        {
            ValidateNumber(number);
            var existing = await _repository.GetByNumberAsync(item.SourceMaterialId, number, cancellationToken);
            if (existing is not null && existing.Id != item.Id)
            {
                throw new ArgumentException($"Unit '{number}' already exists for this source material.", nameof(request.Number));
            }

            item.Number = number;
        }

        if (request.Title is not null)
        {
            item.Title = NormalizeTitle(request.Title);
        }

        _repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SourceMaterialUnitResponse.FromEntity(item);
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
    /// Ensures the unit number is a positive integer.
    /// </summary>
    /// <param name="number">The unit number to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the number is less than 1.</exception>
    private static void ValidateNumber(int number)
    {
        if (number < 1)
        {
            throw new ArgumentException("Unit number must be at least 1.", nameof(number));
        }
    }

    /// <summary>
    /// Trims the title and treats blank titles as absent.
    /// </summary>
    /// <param name="title">The title to normalize, or <c>null</c>.</param>
    /// <returns>The trimmed title, or <c>null</c> when blank or missing.</returns>
    private static string? NormalizeTitle(string? title) => string.IsNullOrWhiteSpace(title) ? null : title.Trim();
}
