using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Tests;

public sealed class SourceMaterialEndpointsTests : ApiTestBase
{
    public SourceMaterialEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ClearSourceMaterials();
    }

    [Fact]
    public async Task GetSourceMaterials_AsAnonymous_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/source-materials");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<SourceMaterialResponse>>();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateSourceMaterial_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/source-materials",
            new CreateSourceMaterialRequest("Rogue One", Medium.Movie, CanonType.Canon));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSourceMaterial_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/source-materials",
            new CreateSourceMaterialRequest("Rogue One", Medium.Movie, CanonType.Canon));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSourceMaterial_AsAdmin_ThenGet_ReturnsCreatedItem()
    {
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync(
            "/api/source-materials",
            new CreateSourceMaterialRequest("A New Hope", Medium.Movie, CanonType.CanonAndLegends));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>();

        Assert.NotNull(created);
        Assert.Equal("A New Hope", created.Title);
        Assert.Equal(Medium.Movie, created.Medium);
        Assert.Equal(CanonType.CanonAndLegends, created.CanonType);

        var getResponse = await Client.GetAsync($"/api/source-materials/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Title, fetched.Title);
    }

    [Fact]
    public async Task GetAll_AsAnonymous_IncludesAdminCreatedSourceMaterials()
    {
        await CreateSourceMaterialAsync("A New Hope");
        await CreateSourceMaterialAsync("The Empire Strikes Back");

        var response = await Client.GetAsync("/api/source-materials");
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<SourceMaterialResponse>>();

        Assert.Contains(items!, x => x.Title == "A New Hope");
        Assert.Contains(items!, x => x.Title == "The Empire Strikes Back");
    }

    [Fact]
    public async Task UpdateSourceMaterial_AsAdmin_ChangesTitleAndCanonType()
    {
        var created = await CreateSourceMaterialAsync("Update me");

        var client = await CreateAdminClientAsync();
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/source-materials/{created.Id}",
            new UpdateSourceMaterialRequest("Updated title", Medium.Book, CanonType.Legends));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal(Medium.Book, updated.Medium);
        Assert.Equal(CanonType.Legends, updated.CanonType);
        Assert.Equal(created.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateSourceMaterial_AsStandardUser_ReturnsForbidden()
    {
        var created = await CreateSourceMaterialAsync("Update me");

        var client = await CreateStandardClientAsync();
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/source-materials/{created.Id}",
            new UpdateSourceMaterialRequest("Nope", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSourceMaterial_AsAdmin_ThenGet_ReturnsNotFound()
    {
        var created = await CreateSourceMaterialAsync("Delete me");

        var client = await CreateAdminClientAsync();
        var deleteResponse = await client.DeleteAsync($"/api/source-materials/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/source-materials/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSourceMaterial_AsStandardUser_ReturnsForbidden()
    {
        var created = await CreateSourceMaterialAsync("Delete me");

        var client = await CreateStandardClientAsync();
        var deleteResponse = await client.DeleteAsync($"/api/source-materials/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetMissingSourceMaterial_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/source-materials/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
