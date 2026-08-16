using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence.Repositories;

/// <summary>
/// EF Core-backed implementation of <see cref="IUserRepository"/>.
/// </summary>
/// <remarks>
/// Reads never include the user's library collection, keeping each query to the minimal set of columns.
/// </remarks>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates a new instance of the <see cref="UserRepository"/>.
    /// </summary>
    /// <param name="context">The database context to query.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    /// <inheritdoc />
    public async Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.EmailVerificationTokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        _context.Users.AddAsync(user, cancellationToken).AsTask();

    /// <inheritdoc />
    public void Update(User user) => _context.Users.Update(user);
}
