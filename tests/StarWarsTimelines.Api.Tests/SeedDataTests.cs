using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Persistence;

namespace StarWarsTimelines.Api.Tests;

public sealed class SeedDataTests : ApiTestBase
{
    public SeedDataTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public void ReseedingRestoresEventUnitLinksAndUnitsOnLegacyDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seededLinks = db.SourceMaterialEvents
            .Where(e => e.SourceMaterialUnitId != null)
            .Select(e => new { e.Id, e.SourceMaterialUnitId })
            .ToList();

        Assert.NotEmpty(seededLinks);

        db.Database.ExecuteSqlRaw("DELETE FROM UserSourceMaterialUnits");
        db.Database.ExecuteSqlRaw("UPDATE SourceMaterialEvents SET SourceMaterialUnitId = NULL");
        db.Database.ExecuteSqlRaw("DELETE FROM SourceMaterialUnits");
        db.ChangeTracker.Clear();

        SeedData.Seed(db);

        foreach (var link in seededLinks)
        {
            var restored = db.SourceMaterialEvents.FirstOrDefault(e => e.Id == link.Id);
            Assert.NotNull(restored);
            Assert.NotNull(restored!.SourceMaterialUnitId);
            Assert.Equal(link.SourceMaterialUnitId, restored.SourceMaterialUnitId);
        }

        Assert.Equal(seededLinks.Count, db.SourceMaterialEvents.Count(e => e.SourceMaterialUnitId != null));
    }
}
