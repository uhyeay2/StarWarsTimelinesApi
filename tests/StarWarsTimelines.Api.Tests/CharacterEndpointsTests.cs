using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Tests;

public sealed class CharacterEndpointsTests : ApiTestBase
{
    public CharacterEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ClearCharacters();
    }

    [Fact]
    public async Task GetCharacters_AsAnonymous_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/characters");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<CharacterResponse>>();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateCharacter_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCharacter_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCharacter_AsAdmin_ThenGet_ReturnsCreatedItem()
    {
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CharacterResponse>();

        Assert.NotNull(created);
        Assert.Equal("Luke Skywalker", created.Name);

        var getResponse = await Client.GetAsync($"/api/characters/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<CharacterResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Name, fetched.Name);
    }

    [Fact]
    public async Task CreateCharacter_AsAdmin_WithBlankName_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCharacter_AsAdmin_ChangesName()
    {
        var created = await CreateCharacterAsync("Old name");

        var client = await CreateAdminClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/characters/{created.Id}", new UpdateCharacterRequest("New name"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CharacterResponse>();

        Assert.NotNull(updated);
        Assert.Equal("New name", updated.Name);
        Assert.Equal(created.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateCharacter_AsStandardUser_ReturnsForbidden()
    {
        var created = await CreateCharacterAsync("Keep me");

        var client = await CreateStandardClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/characters/{created.Id}", new UpdateCharacterRequest("Nope"));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCharacter_AsAdmin_ThenGet_ReturnsNotFound()
    {
        var created = await CreateCharacterAsync("Delete me");

        var client = await CreateAdminClientAsync();
        var deleteResponse = await client.DeleteAsync($"/api/characters/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/characters/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetMissingCharacter_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/characters/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<CharacterResponse> CreateCharacterAsync(string name)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CharacterResponse>())!;
    }
}
