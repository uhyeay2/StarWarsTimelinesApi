using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed repository for <see cref="RefreshToken"/> records.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="RefreshTokenRepository"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAtUtc, DateTime.UtcNow),
                cancellationToken);
    }
}
