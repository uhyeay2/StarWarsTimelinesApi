using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Application;

/// <summary>
/// Application service that manages the vehicle catalog.
/// </summary>
public sealed class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates a new instance of the <see cref="VehicleService"/>.
    /// </summary>
    /// <param name="repository">The repository used to persist vehicles.</param>
    /// <param name="unitOfWork">The unit of work used to commit changes.</param>
    public VehicleService(IVehicleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VehicleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(VehicleResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : VehicleResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<VehicleResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var item = new Vehicle
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        await _repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return VehicleResponse.FromEntity(item);
    }

    /// <inheritdoc />
    public async Task<VehicleResponse?> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
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

        return VehicleResponse.FromEntity(item);
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
}
