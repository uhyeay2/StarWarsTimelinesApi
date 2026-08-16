using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ISourceMaterialUnitRepository"/>.
/// </summary>
/// <remarks>
/// All reads use <c>AsNoTracking()</c> and never include related data, so only the minimal columns are loaded.
/// </remarks>
public sealed class SourceMaterialUnitRepository : ISourceMaterialUnitRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="SourceMaterialUnitRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public SourceMaterialUnitRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SourceMaterialUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterialUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SourceMaterialUnit>> GetBySourceMaterialAsync(Guid sourceMaterialId, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterialUnits
            .AsNoTracking()
            .Where(x => x.SourceMaterialId == sourceMaterialId)
            .OrderBy(x => x.GroupNumber)
            .ThenBy(x => x.Number)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<SourceMaterialUnit?> GetByNumberAsync(Guid sourceMaterialId, int? groupNumber, int number, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterialUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.SourceMaterialId == sourceMaterialId && x.GroupNumber == groupNumber && x.Number == number,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(SourceMaterialUnit item, CancellationToken cancellationToken = default) =>
        await _context.SourceMaterialUnits.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(SourceMaterialUnit item) => _context.SourceMaterialUnits.Update(item);

    /// <inheritdoc />
    public void Remove(SourceMaterialUnit item) => _context.SourceMaterialUnits.Remove(item);
}
