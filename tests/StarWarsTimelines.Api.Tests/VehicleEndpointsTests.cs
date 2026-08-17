using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Tests;

public sealed class VehicleEndpointsTests : ApiTestBase
{
    public VehicleEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ClearVehicles();
    }

    [Fact]
    public async Task GetVehicles_AsAnonymous_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/vehicles");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<VehicleResponse>>();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateVehicle_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("Millennium Falcon"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("Millennium Falcon"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_AsAdmin_ThenGet_ReturnsCreatedItem()
    {
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("Millennium Falcon"));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        Assert.NotNull(created);
        Assert.Equal("Millennium Falcon", created.Name);

        var getResponse = await Client.GetAsync($"/api/vehicles/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Name, fetched.Name);
    }

    [Fact]
    public async Task CreateVehicle_AsAdmin_WithBlankName_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVehicle_AsAdmin_ChangesName()
    {
        var created = await CreateVehicleAsync("Old name");

        var client = await CreateAdminClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/vehicles/{created.Id}", new UpdateVehicleRequest("New name"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        Assert.NotNull(updated);
        Assert.Equal("New name", updated.Name);
        Assert.Equal(created.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateVehicle_AsStandardUser_ReturnsForbidden()
    {
        var created = await CreateVehicleAsync("Keep me");

        var client = await CreateStandardClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/vehicles/{created.Id}", new UpdateVehicleRequest("Nope"));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteVehicle_AsAdmin_ThenGet_ReturnsNotFound()
    {
        var created = await CreateVehicleAsync("Delete me");

        var client = await CreateAdminClientAsync();
        var deleteResponse = await client.DeleteAsync($"/api/vehicles/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/vehicles/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteVehicle_WhenLinkedToEvent_ReturnsConflict()
    {
        var created = await CreateVehicleAsync("Linked vehicle");
        var client = await CreateAdminClientAsync();
        var source = await CreateSourceMaterialAsync("Conflict Test Material");

        var eventResponse = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest(
                "Conflict Test Event", "desc", CanonType.Canon, 0, "0 BBY", null,
                source.Id, null, [], [], [created.Id]));
        eventResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/vehicles/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/vehicles/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetMissingVehicle_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/vehicles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<VehicleResponse> CreateVehicleAsync(string name)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VehicleResponse>())!;
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
