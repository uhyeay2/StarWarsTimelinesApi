using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;
using StarWarsTimelines.Persistence;

namespace StarWarsTimelines.Api.Tests;

public sealed class SourceMaterialUnitEndpointsTests : ApiTestBase
{
    private static readonly Guid MandalorianId = new("00000000-0000-0000-0000-000000000012");
    private static readonly Guid MandalorianUnitOneId = new("00000000-0000-0000-0000-500000000025");

    public SourceMaterialUnitEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ResetScratchUnits();
    }

    [Fact]
    public async Task GetUnits_AsAnonymous_ReturnsSeededUnitsOrderedByNumber()
    {
        var response = await Client.GetAsync($"/api/source-materials/{MandalorianId}/units");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<SourceMaterialUnitResponse>>();

        Assert.NotNull(items);
        var episodes = items.Where(u => u.UnitType == UnitType.Episode).ToList();
        Assert.Equal(8, episodes.Count);
        Assert.Equal(Enumerable.Range(1, 8), episodes.Select(x => x.Number));
        Assert.All(episodes, x => Assert.Equal(MandalorianId, x.SourceMaterialId));
        var first = episodes[0];
        Assert.Equal("Chapter 1: The Mandalorian", first.Title);
    }

    [Fact]
    public async Task GetUnits_ForUnknownMaterial_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/source-materials/{Guid.NewGuid()}/units");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 9, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 9, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_AsAdmin_CreatesUnit()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 9, "Chapter 9: The Marshal"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<SourceMaterialUnitResponse>();

        Assert.NotNull(created);
        Assert.Equal(MandalorianId, created.SourceMaterialId);
        Assert.Equal(UnitType.Episode, created.UnitType);
        Assert.Equal(9, created.Number);
        Assert.Equal("Chapter 9: The Marshal", created.Title);
    }

    [Fact]
    public async Task CreateUnit_AsAdmin_ForUnknownMaterial_ReturnsNotFound()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/source-materials/{Guid.NewGuid()}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 1, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_AsAdmin_WithDuplicateNumber_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 1, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_AsAdmin_WithNumberLessThanOne_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, 0, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUnit_AsAdmin_ChangesFields()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateUnitAsync(MandalorianId, 20);

        var response = await client.PutAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units/{created.Id}",
            new UpdateSourceMaterialUnitRequest(UnitType.Chapter, 1, 21, "Renamed"));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<SourceMaterialUnitResponse>();

        Assert.NotNull(updated);
        Assert.Equal(UnitType.Chapter, updated.UnitType);
        Assert.Equal(21, updated.Number);
        Assert.Equal("Renamed", updated.Title);
    }

    [Fact]
    public async Task UpdateUnit_AsAdmin_WhenMissing_ReturnsNotFound()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units/{Guid.NewGuid()}",
            new UpdateSourceMaterialUnitRequest(null, null, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUnit_AsAdmin_WithDuplicateNumber_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();
        await CreateUnitAsync(MandalorianId, 20);

        var response = await client.PutAsJsonAsync(
            $"/api/source-materials/{MandalorianId}/units/{MandalorianUnitOneId}",
            new UpdateSourceMaterialUnitRequest(null, 1, 20, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUnit_AsAdmin_ThenGet_ReturnsNotFound()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateUnitAsync(MandalorianId, 20);

        var deleteResponse = await client.DeleteAsync($"/api/source-materials/{MandalorianId}/units/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/source-materials/{MandalorianId}/units");
        var items = (await getResponse.Content.ReadFromJsonAsync<List<SourceMaterialUnitResponse>>())!;
        Assert.DoesNotContain(items, x => x.Id == created.Id);
    }

    [Fact]
    public async Task DeleteUnit_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.DeleteAsync($"/api/source-materials/{MandalorianId}/units/{MandalorianUnitOneId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUnit_WhenMissing_ReturnsNotFound()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.DeleteAsync($"/api/source-materials/{MandalorianId}/units/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUnit_WhenReferencedByEvent_ReturnsConflict()
    {
        var client = await CreateAdminClientAsync();

        var eventResponse = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest(
                "Conflict Test Event", "desc", CanonType.Canon, 0, "0 BBY", null,
                MandalorianId, MandalorianUnitOneId, [], [], []));
        eventResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/source-materials/{MandalorianId}/units/{MandalorianUnitOneId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/source-materials/{MandalorianId}/units");
        var items = (await getResponse.Content.ReadFromJsonAsync<List<SourceMaterialUnitResponse>>())!;
        Assert.Contains(items, x => x.Id == MandalorianUnitOneId);
    }

    private async Task<SourceMaterialUnitResponse> CreateUnitAsync(Guid sourceMaterialId, int number)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/source-materials/{sourceMaterialId}/units",
            new CreateSourceMaterialUnitRequest(UnitType.Episode, 1, number, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SourceMaterialUnitResponse>())!;
    }

    /// <summary>
    /// Removes any scratch units created by other tests in this class so every test starts from the seeded
    /// Mandalorian episode list (numbers 1-8).
    /// </summary>
    private void ResetScratchUnits()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM UserSourceMaterialUnits");
        db.Database.ExecuteSql($"DELETE FROM SourceMaterialUnits WHERE SourceMaterialId = {MandalorianId} AND Number > 8");
    }
}
