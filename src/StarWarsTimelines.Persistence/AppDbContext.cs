using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Domain.Entities;

namespace StarWarsTimelines.Persistence;

/// <summary>
/// The Entity Framework Core database context for the application, which also acts as the unit of work.
/// </summary>
public sealed class AppDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// Creates a new instance of the <see cref="AppDbContext"/>.
    /// </summary>
    /// <param name="options">The options used to configure the context, including the connection string.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the catalog of source materials.
    /// </summary>
    public DbSet<SourceMaterial> SourceMaterials => Set<SourceMaterial>();

    /// <summary>
    /// Gets the registered user accounts.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the per-user library items linking users to tracked source materials.
    /// </summary>
    public DbSet<UserSourceMaterial> UserSourceMaterials => Set<UserSourceMaterial>();

    /// <summary>
    /// Gets the catalog of sub-units (episodes, chapters, issues, and levels) that source materials are divided into.
    /// </summary>
    public DbSet<SourceMaterialUnit> SourceMaterialUnits => Set<SourceMaterialUnit>();

    /// <summary>
    /// Gets the per-user progress records linking users to completed source material units.
    /// </summary>
    public DbSet<UserSourceMaterialUnit> UserSourceMaterialUnits => Set<UserSourceMaterialUnit>();

    /// <summary>
    /// Gets the catalog of characters that can be linked to timeline events.
    /// </summary>
    public DbSet<Character> Characters => Set<Character>();

    /// <summary>
    /// Gets the catalog of species that characters can belong to.
    /// </summary>
    public DbSet<Species> Species => Set<Species>();

    /// <summary>
    /// Gets the catalog of locations that can be linked to timeline events.
    /// </summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>
    /// Gets the catalog of vehicles that can be linked to timeline events.
    /// </summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    /// <summary>
    /// Gets the catalog of timeline events.
    /// </summary>
    public DbSet<SourceMaterialEvent> SourceMaterialEvents => Set<SourceMaterialEvent>();

    /// <summary>
    /// Gets the links between timeline events and characters.
    /// </summary>
    public DbSet<EventCharacter> EventCharacters => Set<EventCharacter>();

    /// <summary>
    /// Gets the links between timeline events and locations.
    /// </summary>
    public DbSet<EventLocation> EventLocations => Set<EventLocation>();

    /// <summary>
    /// Gets the links between timeline events and vehicles.
    /// </summary>
    public DbSet<EventVehicle> EventVehicles => Set<EventVehicle>();

    /// <summary>
    /// Gets the refresh tokens used for token rotation.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Applies the entity configurations defined in this assembly to the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to construct the EF Core model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        base.SaveChangesAsync(cancellationToken);
}
