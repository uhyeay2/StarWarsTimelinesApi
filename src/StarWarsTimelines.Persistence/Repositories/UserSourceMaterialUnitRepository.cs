using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="IUserSourceMaterialUnitRepository"/>.
/// </summary>
/// <remarks>
/// All reads use <c>AsNoTracking()</c> and never include related data, so only the minimal columns are loaded.
/// </remarks>
public sealed class UserSourceMaterialUnitRepository : IUserSourceMaterialUnitRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="UserSourceMaterialUnitRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public UserSourceMaterialUnitRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSourceMaterialUnit>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterialUnits
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<UserSourceMaterialUnit?> GetByIdAsync(Guid userId, Guid sourceMaterialUnitId, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterialUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.SourceMaterialUnitId == sourceMaterialUnitId,
                cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(UserSourceMaterialUnit item, CancellationToken cancellationToken = default) =>
        await _context.UserSourceMaterialUnits.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(UserSourceMaterialUnit item) => _context.UserSourceMaterialUnits.Update(item);
}
