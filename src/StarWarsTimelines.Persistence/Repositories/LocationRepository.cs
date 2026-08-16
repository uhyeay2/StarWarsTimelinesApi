using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ILocationRepository"/>.
/// </summary>
/// <remarks>
/// All reads use <c>AsNoTracking()</c> and never include related data, so only the minimal columns are loaded.
/// </remarks>
public sealed class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="LocationRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Locations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Location item, CancellationToken cancellationToken = default) =>
        await _context.Locations.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(Location item) => _context.Locations.Update(item);

    /// <inheritdoc />
    public void Remove(Location item) => _context.Locations.Remove(item);
}
