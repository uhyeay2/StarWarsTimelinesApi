using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Tests;

public sealed class SpeciesEndpointsTests : ApiTestBase
{
    public SpeciesEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ClearSpecies();
    }

    [Fact]
    public async Task GetSpecies_AsAnonymous_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/species");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<SpeciesResponse>>();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateSpecies_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest("Twi'lek"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSpecies_AsStandardUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest("Twi'lek"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSpecies_AsAdmin_WithHomePlanet_ThenGet_ReturnsCreatedItem()
    {
        // Location names must not collide with the seeded catalog entries.
        var planet = await CreateLocationAsync("Ryloth Prime");
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest("Twi'lek", planet.Id));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.NotNull(created);
        Assert.Equal("Twi'lek", created.Name);
        Assert.Equal(planet.Id, created.HomePlanetId);
        Assert.Equal("Ryloth Prime", created.HomePlanetName);

        var getResponse = await Client.GetAsync($"/api/species/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<SpeciesResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Ryloth Prime", fetched.HomePlanetName);
    }

    [Fact]
    public async Task CreateSpecies_AsAdmin_WithoutHomePlanet_ReturnsWithUnknownHomePlanet()
    {
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest("Yoda's species"));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.NotNull(created);
        Assert.Null(created.HomePlanetId);
        Assert.Null(created.HomePlanetName);
    }

    [Fact]
    public async Task CreateSpecies_AsAdmin_WithBlankName_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSpecies_AsAdmin_WithUnknownHomePlanet_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest("Mirialan", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSpecies_AsAdmin_ChangesNameAndHomePlanet()
    {
        var created = await CreateSpeciesAsync("Zabrak");
        var planet = await CreateLocationAsync("Iridonia Prime");
        var client = await CreateAdminClientAsync();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/species/{created.Id}", new UpdateSpeciesRequest("Iridonian Zabrak", planet.Id));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Iridonian Zabrak", updated.Name);
        Assert.Equal(planet.Id, updated.HomePlanetId);
        Assert.Equal("Iridonia Prime", updated.HomePlanetName);
    }

    [Fact]
    public async Task UpdateSpecies_AsStandardUser_ReturnsForbidden()
    {
        var created = await CreateSpeciesAsync("Keep me");

        var client = await CreateStandardClientAsync();
        var updateResponse = await client.PutAsJsonAsync($"/api/species/{created.Id}", new UpdateSpeciesRequest("Nope"));

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSpecies_AsAdmin_ClearsReferencingCharacter()
    {
        // The character name must not collide with a seeded catalog entry.
        var species = await CreateSpeciesAsync("Human");
        var character = await CreateCharacterAsync(new CreateCharacterRequest("Species Test Human", SpeciesId: species.Id));
        var client = await CreateAdminClientAsync();

        var deleteResponse = await client.DeleteAsync($"/api/species/{species.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/species/{species.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // The character survives with its species reference cleared back to unknown (ON DELETE SET NULL).
        var characterResponse = await Client.GetAsync($"/api/characters/{character.Id}");
        characterResponse.EnsureSuccessStatusCode();
        var fetched = await characterResponse.Content.ReadFromJsonAsync<CharacterResponse>();
        Assert.NotNull(fetched);
        Assert.Null(fetched.SpeciesId);
        Assert.Null(fetched.SpeciesName);
    }

    [Fact]
    public async Task GetMissingSpecies_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/species/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<LocationResponse> CreateLocationAsync(string name)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<SpeciesResponse> CreateSpeciesAsync(string name)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SpeciesResponse>())!;
    }

    private async Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/characters", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CharacterResponse>())!;
    }
}
