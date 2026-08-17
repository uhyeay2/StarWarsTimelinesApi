using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="IVehicleRepository"/>.
/// </summary>
/// <remarks>
/// All reads use <c>AsNoTracking()</c> and never include related data, so only the minimal columns are loaded.
/// </remarks>
public sealed class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="VehicleRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Vehicles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Vehicle item, CancellationToken cancellationToken = default) =>
        await _context.Vehicles.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(Vehicle item) => _context.Vehicles.Update(item);

    /// <inheritdoc />
    public void Remove(Vehicle item) => _context.Vehicles.Remove(item);

    /// <inheritdoc />
    public async Task<bool> IsReferencedByEventAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.EventVehicles.AsNoTracking().AnyAsync(x => x.VehicleId == id, cancellationToken);
}
