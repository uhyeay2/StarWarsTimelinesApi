using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ISpeciesRepository"/>.
/// </summary>
/// <remarks>
/// Reads use <c>AsNoTracking()</c> and include the home planet so responses can carry its name.
/// </remarks>
public sealed class SpeciesRepository : ISpeciesRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="SpeciesRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public SpeciesRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<Species?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Species
            .AsNoTracking()
            .Include(x => x.HomePlanet)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Species>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Species
            .AsNoTracking()
            .Include(x => x.HomePlanet)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Species item, CancellationToken cancellationToken = default) =>
        await _context.Species.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(Species item) => _context.Species.Update(item);

    /// <inheritdoc />
    public void Remove(Species item) => _context.Species.Remove(item);
}
