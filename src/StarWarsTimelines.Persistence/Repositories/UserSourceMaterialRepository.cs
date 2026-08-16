using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="IUserSourceMaterialRepository"/>.
/// </summary>
/// <remarks>
/// Library queries include only the tracked <see cref="SourceMaterial"/> navigation and its
/// <see cref="SourceMaterial.SourceMaterialUnits"/> (both required to render a library item with sub-unit progress)
/// and are otherwise minimal. Lazy loading is not enabled.
/// </remarks>
public sealed class UserSourceMaterialRepository : IUserSourceMaterialRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="UserSourceMaterialRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public UserSourceMaterialRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSourceMaterial>> GetLibraryAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterials
            .AsNoTracking()
            .Include(x => x.SourceMaterial)
            .ThenInclude(material => material.SourceMaterialUnits)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSourceMaterial>> GetTrackedItemsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterials
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetNextSortOrderAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var max = await _context.UserSourceMaterials
            .Where(x => x.UserId == userId)
            .MaxAsync(x => (int?)x.SortOrder, cancellationToken);
        return (max ?? -1) + 1;
    }

    /// <inheritdoc />
    public async Task<UserSourceMaterial?> GetByIdAsync(Guid userId, Guid sourceMaterialId, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterials
            .Include(x => x.SourceMaterial)
            .ThenInclude(material => material.SourceMaterialUnits)
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.SourceMaterialId == sourceMaterialId,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(UserSourceMaterial item, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterials.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(UserSourceMaterial item) => _context.UserSourceMaterials.Update(item);

    /// <inheritdoc />
    public void Remove(UserSourceMaterial item) => _context.UserSourceMaterials.Remove(item);
}
