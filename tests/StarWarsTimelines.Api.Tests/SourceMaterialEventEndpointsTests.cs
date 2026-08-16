using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Tests;

public sealed class SourceMaterialEventEndpointsTests : ApiTestBase
{
    private static readonly string[] SeededTitles =
    [
        "Origins of the Jedi Order",
        "Revan and the Exile of the Sith",
        "The Ruusan Reformation",
        "The Invasion of Naboo",
        "The Battle of Geonosis",
        "The Siege of Mandalore",
        "Order 66",
        "The Destruction of Alderaan",
        "The Battle of Yavin",
        "The Battle of Hoth",
        "The Battle of Endor",
        "The Rescue",
        "The Battle of Exegol",
        "The Second Galactic Civil War",
        "The Great Hyperspace Disaster",
        "The Descent of the Je'daii",
        "The Holocron Heist",
        "The Battle of Lothal",
        "The Search for Thrawn",
        "The Wounded Jedi",
        "The Fall of Bracca"
    ];

    public SourceMaterialEventEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetEvents_AsAnonymous_ReturnsAllSeededEvents()
    {
        var response = await Client.GetAsync("/api/source-material-events");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<SourceMaterialEventResponse>>();

        Assert.NotNull(items);
        foreach (var title in SeededTitles)
        {
            Assert.Contains(items, x => x.Title == title);
        }
    }

    [Fact]
    public async Task GetEvents_AsAnonymous_AreOrderedByYearThenTitle()
    {
        var response = await Client.GetAsync("/api/source-material-events");
        var items = (await response.Content.ReadFromJsonAsync<List<SourceMaterialEventResponse>>())!;

        var expected = items.OrderBy(x => x.Year).ThenBy(x => x.Title, StringComparer.Ordinal).Select(x => x.Id);
        var actual = items.Select(x => x.Id);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetEventById_AsAnonymous_ReturnsEventWithNestedLinks()
    {
        var yavin = await GetEventByTitleAsync("The Battle of Yavin");

        var response = await Client.GetAsync($"/api/source-material-events/{yavin.Id}");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<SourceMaterialEventResponse>();

        Assert.NotNull(fetched);
        Assert.Equal(yavin.Id, fetched.Id);
        Assert.Equal("Star Wars: Episode IV - A New Hope", fetched.SourceMaterial.Title);
        Assert.Equal(CanonType.CanonAndLegends, fetched.CanonType);
        Assert.Contains(fetched.Characters, x => x.Name == "Luke Skywalker");
        Assert.Contains(fetched.Locations, x => x.Name == "Yavin 4");
        Assert.Contains(fetched.Vehicles, x => x.Name == "Millennium Falcon");
    }

    [Fact]
    public async Task GetMissingEvent_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/source-material-events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest("Test", "desc", CanonType.Canon, 0, "0 BBY", null, Guid.NewGuid(), null, [], [], []));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest("Test", "desc", CanonType.Canon, 0, "0 BBY", null, Guid.NewGuid(), null, [], [], []));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsAdmin_WithValidLinks_ReturnsCreated()
    {
        var client = await CreateAdminClientAsync();
        var source = await GetSourceMaterialByTitleAsync("Star Wars: Episode IV - A New Hope");
        var luke = (await GetCharacterByNameAsync("Luke Skywalker")).Id;
        var yavin4 = (await GetLocationByNameAsync("Yavin 4")).Id;
        var falcon = (await GetVehicleByNameAsync("Millennium Falcon")).Id;

        var createdResponse = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest(
                "The Attack on the First Death Star",
                "Rebel pilots strike the superweapon.",
                CanonType.CanonAndLegends,
                0,
                "0 BBY",
                null,
                source.Id,
                null,
                [luke],
                [yavin4],
                [falcon]));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<SourceMaterialEventResponse>();

        Assert.NotNull(created);
        Assert.Equal("The Attack on the First Death Star", created.Title);
        Assert.Single(created.Characters);
        Assert.Equal("Luke Skywalker", created.Characters[0].Name);
        Assert.Equal("Star Wars: Episode IV - A New Hope", created.SourceMaterial.Title);
    }

    [Fact]
    public async Task CreateEvent_AsAdmin_WhenSourceMaterialMissing_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest("Test", "desc", CanonType.Canon, 0, "0 BBY", null, Guid.NewGuid(), null, [], [], []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsAdmin_WhenCharacterMissing_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var source = await GetSourceMaterialByTitleAsync("Star Wars: Episode IV - A New Hope");

        var response = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest("Test", "desc", CanonType.Canon, 0, "0 BBY", null, source.Id, null, [Guid.NewGuid()], [], []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsAdmin_WithValidUnitLink_ReturnsUnit()
    {
        var client = await CreateAdminClientAsync();
        var mandalorian = await GetSourceMaterialByTitleAsync("The Mandalorian");
        var unitsResponse = await Client.GetAsync($"/api/source-materials/{mandalorian.Id}/units");
        var units = (await unitsResponse.Content.ReadFromJsonAsync<List<SourceMaterialUnitResponse>>())!;

        var createdResponse = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest("The Marshal's Last Job", "A job goes wrong on Nevarro.", CanonType.Canon, 9, "9 ABY", null, mandalorian.Id, units[0].Id, [], [], []));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<SourceMaterialEventResponse>();

        Assert.NotNull(created);
        Assert.NotNull(created!.SourceMaterialUnit);
        Assert.Equal(units[0].Id, created.SourceMaterialUnit.Id);
        Assert.Equal("The Mandalorian", created.SourceMaterial.Title);
    }

    [Fact]
    public async Task CreateEvent_AsAdmin_WithUnitFromAnotherMaterial_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var source = await GetSourceMaterialByTitleAsync("Star Wars: Episode IV - A New Hope");
        var mandalorian = await GetSourceMaterialByTitleAsync("The Mandalorian");
        var unitsResponse = await Client.GetAsync($"/api/source-materials/{mandalorian.Id}/units");
        var units = (await unitsResponse.Content.ReadFromJsonAsync<List<SourceMaterialUnitResponse>>())!;

        var response = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest("Test", "desc", CanonType.Canon, 0, "0 BBY", null, source.Id, units[0].Id, [], [], []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsAdmin_ChangesTitleAndLinks()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateEventAsync("Update me");
        var hoth = (await GetLocationByNameAsync("Hoth")).Id;

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/source-material-events/{created.Id}",
            new UpdateSourceMaterialEventRequest("Updated title", null, null, null, null, null, null, null, null, [hoth], null));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<SourceMaterialEventResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated.Title);
        Assert.Contains(updated.Locations, x => x.Name == "Hoth");
    }

    [Fact]
    public async Task UpdateEvent_AsAdmin_WhenCharacterMissing_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateEventAsync("Update me");

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/source-material-events/{created.Id}",
            new UpdateSourceMaterialEventRequest(null, null, null, null, null, null, null, null, [Guid.NewGuid()], null, null));

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsAdmin_ThenGet_ReturnsNotFound()
    {
        var client = await CreateAdminClientAsync();
        var created = await CreateEventAsync("Delete me");

        var deleteResponse = await client.DeleteAsync($"/api/source-material-events/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/source-material-events/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<SourceMaterialEventResponse> CreateEventAsync(string title)
    {
        var client = await CreateAdminClientAsync();
        var source = await GetSourceMaterialByTitleAsync("Star Wars: Episode IV - A New Hope");
        var response = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest(title, "desc", CanonType.CanonAndLegends, 0, "0 BBY", null, source.Id, null, [], [], []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SourceMaterialEventResponse>())!;
    }

    private async Task<SourceMaterialEventResponse> GetEventByTitleAsync(string title)
    {
        var response = await Client.GetAsync("/api/source-material-events");
        var items = (await response.Content.ReadFromJsonAsync<List<SourceMaterialEventResponse>>())!;
        return items.Single(x => x.Title == title);
    }

    private async Task<SourceMaterialResponse> GetSourceMaterialByTitleAsync(string title)
    {
        var response = await Client.GetAsync("/api/source-materials");
        var items = (await response.Content.ReadFromJsonAsync<List<SourceMaterialResponse>>())!;
        return items.Single(x => x.Title == title);
    }

    private async Task<CharacterResponse> GetCharacterByNameAsync(string name)
    {
        var response = await Client.GetAsync("/api/characters");
        var items = (await response.Content.ReadFromJsonAsync<List<CharacterResponse>>())!;
        return items.Single(x => x.Name == name);
    }

    private async Task<LocationResponse> GetLocationByNameAsync(string name)
    {
        var response = await Client.GetAsync("/api/locations");
        var items = (await response.Content.ReadFromJsonAsync<List<LocationResponse>>())!;
        return items.Single(x => x.Name == name);
    }

    private async Task<VehicleResponse> GetVehicleByNameAsync(string name)
    {
        var response = await Client.GetAsync("/api/vehicles");
        var items = (await response.Content.ReadFromJsonAsync<List<VehicleResponse>>())!;
        return items.Single(x => x.Name == name);
    }
}
