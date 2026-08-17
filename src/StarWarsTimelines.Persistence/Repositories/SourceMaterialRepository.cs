using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ISourceMaterialRepository"/>.
/// </summary>
/// <remarks>
/// All reads use <c>AsNoTracking()</c> and never include related data, so only the minimal columns are loaded.
/// </remarks>
public sealed class SourceMaterialRepository : ISourceMaterialRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="SourceMaterialRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public SourceMaterialRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SourceMaterial?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceMaterial>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.SourceMaterials.AsNoTracking().OrderBy(x => x.Title).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SourceMaterial item, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterials.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(SourceMaterial item) => _context.SourceMaterials.Update(item);

    /// <inheritdoc />
    public void Remove(SourceMaterial item) => _context.SourceMaterials.Remove(item);

    /// <inheritdoc />
    public async Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var referencedByEvent = await _context.SourceMaterialEvents
            .AsNoTracking()
            .AnyAsync(x => x.SourceMaterialId == id, cancellationToken);
        if (referencedByEvent)
        {
            return true;
        }

        var referencedByLibrary = await _context.UserSourceMaterials
            .AsNoTracking()
            .AnyAsync(x => x.SourceMaterialId == id, cancellationToken);
        if (referencedByLibrary)
        {
            return true;
        }

        var unitIds = _context.SourceMaterialUnits
            .Where(u => u.SourceMaterialId == id)
            .Select(u => u.Id);

        var unitReferencedByEvent = await _context.SourceMaterialEvents
            .AsNoTracking()
            .AnyAsync(x => x.SourceMaterialUnitId != null && unitIds.Contains(x.SourceMaterialUnitId.Value), cancellationToken);
        if (unitReferencedByEvent)
        {
            return true;
        }

        return await _context.UserSourceMaterialUnits
            .AsNoTracking()
            .AnyAsync(x => unitIds.Contains(x.SourceMaterialUnitId), cancellationToken);
    }
}
