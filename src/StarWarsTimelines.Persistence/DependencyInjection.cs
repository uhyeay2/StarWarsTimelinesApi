using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Persistence.Repositories;

namespace StarWarsTimelines.Persistence;

/// <summary>
/// Registers the persistence layer services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the EF Core database context, repositories, and unit of work to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ISourceMaterialRepository, SourceMaterialRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSourceMaterialRepository, UserSourceMaterialRepository>();
        services.AddScoped<ISourceMaterialUnitRepository, SourceMaterialUnitRepository>();
        services.AddScoped<IUserSourceMaterialUnitRepository, UserSourceMaterialUnitRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<ISourceMaterialEventRepository, SourceMaterialEventRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}
