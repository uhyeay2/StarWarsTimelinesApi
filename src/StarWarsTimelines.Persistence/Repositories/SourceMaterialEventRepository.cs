using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ISourceMaterialEventRepository"/>.
/// </summary>
/// <remarks>
/// Read queries use <c>AsNoTracking()</c> and always include the source material and every linked character,
/// location, and vehicle so responses can be built without additional queries. The tracked read is only used when
/// an event's link collections must be edited and saved.
/// </remarks>
public sealed class SourceMaterialEventRepository : ISourceMaterialEventRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="SourceMaterialEventRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public SourceMaterialEventRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceMaterialEvent>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Events().OrderBy(x => x.Year).ThenBy(x => x.Title).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<SourceMaterialEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Events().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<SourceMaterialEvent?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Events(tracked: true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SourceMaterialEvent item, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterialEvents.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(SourceMaterialEvent item) => _context.SourceMaterialEvents.Update(item);

    /// <inheritdoc />
    public void Remove(SourceMaterialEvent item) => _context.SourceMaterialEvents.Remove(item);

    /// <summary>
    /// Builds a query for events that eagerly loads the source material and all linked characters, locations, and
    /// vehicles.
    /// </summary>
    /// <param name="tracked">When <c>true</c>, the query result is tracked so link edits can be saved.</param>
    /// <returns>The base <see cref="IQueryable{T}"/> for events with its navigation paths loaded.</returns>
    private IQueryable<SourceMaterialEvent> Events(bool tracked = false)
    {
        IQueryable<SourceMaterialEvent> query = _context.SourceMaterialEvents;
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(x => x.SourceMaterial)
            .Include(x => x.EventCharacters).ThenInclude(x => x.Character)
            .Include(x => x.EventLocations).ThenInclude(x => x.Location)
            .Include(x => x.EventVehicles).ThenInclude(x => x.Vehicle);
    }
}
