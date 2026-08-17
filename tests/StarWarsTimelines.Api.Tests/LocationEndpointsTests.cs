using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Tests;

public sealed class LocationEndpointsTests : ApiTestBase
{
    public LocationEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ClearLocations();
    }

    [Fact]
    public async Task GetLocations_AsAnonymous_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/locations");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<LocationResponse>>();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateLocation_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("Tython"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLocation_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("Tython"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateLocation_AsAdmin_ThenGet_ReturnsCreatedItem()
    {
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("Tython"));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<LocationResponse>();

        Assert.NotNull(created);
        Assert.Equal("Tython", created.Name);

        var getResponse = await Client.GetAsync($"/api/locations/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Name, fetched.Name);
    }

    [Fact]
    public async Task CreateLocation_AsAdmin_WithBlankName_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLocation_AsAdmin_ChangesName()
    {
        var created = await CreateLocationAsync("Old name");

        var client = await CreateAdminClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/locations/{created.Id}", new UpdateLocationRequest("New name"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<LocationResponse>();

        Assert.NotNull(updated);
        Assert.Equal("New name", updated.Name);
        Assert.Equal(created.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateLocation_AsStandardUser_ReturnsForbidden()
    {
        var created = await CreateLocationAsync("Keep me");

        var client = await CreateStandardClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/locations/{created.Id}", new UpdateLocationRequest("Nope"));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteLocation_AsAdmin_ThenGet_ReturnsNotFound()
    {
        var created = await CreateLocationAsync("Delete me");

        var client = await CreateAdminClientAsync();
        var deleteResponse = await client.DeleteAsync($"/api/locations/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/locations/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteLocation_WhenLinkedToEvent_ReturnsConflict()
    {
        var created = await CreateLocationAsync("Linked location");
        var client = await CreateAdminClientAsync();
        var source = await CreateSourceMaterialAsync("Conflict Test Material");

        var eventResponse = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest(
                "Conflict Test Event", "desc", CanonType.Canon, 0, "0 BBY", null,
                source.Id, null, [], [created.Id], []));
        eventResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/locations/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/locations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetMissingLocation_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/locations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<LocationResponse> CreateLocationAsync(string name)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<SourceMaterialResponse> CreateSourceMaterialAsync(string title)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            "/api/source-materials",
            new CreateSourceMaterialRequest(title, Medium.Movie, CanonType.Canon));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SourceMaterialResponse>())!;
    }
}
