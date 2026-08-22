using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="ICharacterRepository"/>.
/// </summary>
/// <remarks>
/// All reads use <c>AsNoTracking()</c> and include the optional birth planet and species navigations so
/// responses can carry their names; timeline event links are never included.
/// </remarks>
public sealed class CharacterRepository : ICharacterRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="CharacterRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public CharacterRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<Character?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Characters
            .AsNoTracking()
            .Include(x => x.PlanetBornOn)
            .Include(x => x.Species)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Character>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Characters
            .AsNoTracking()
            .Include(x => x.PlanetBornOn)
            .Include(x => x.Species)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Character item, CancellationToken cancellationToken = default) =>
        await _context.Characters.AddAsync(item, cancellationToken);

    /// <inheritdoc />
    public void Update(Character item) => _context.Characters.Update(item);

    /// <inheritdoc />
    public void Remove(Character item) => _context.Characters.Remove(item);

    /// <inheritdoc />
    public async Task<bool> IsReferencedByEventAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.EventCharacters.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
}
