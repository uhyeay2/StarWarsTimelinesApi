using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

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
    public async Task CreateCharacter_AsAdmin_WithBiography_RoundTripsAllAttributes()
    {
        // Location and species names must not collide with the seeded catalog entries.
        var planet = await CreateLocationAsync("Polis Massa Annex");
        var species = await CreateSpeciesAsync("Corellian Human");
        var client = await CreateAdminClientAsync();

        var createdResponse = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest(
            "Luke Skywalker", planet.Id, -19, -19, 34, 34, species.Id));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CharacterResponse>();

        Assert.NotNull(created);
        Assert.Equal(planet.Id, created.PlanetBornOnId);
        Assert.Equal("Polis Massa Annex", created.PlanetBornOnName);
        Assert.Equal(-19, created.YearOfBirthEarliest);
        Assert.Equal(-19, created.YearOfBirthLatest);
        Assert.Equal(34, created.YearOfDeathEarliest);
        Assert.Equal(34, created.YearOfDeathLatest);
        Assert.Equal(species.Id, created.SpeciesId);
        Assert.Equal("Corellian Human", created.SpeciesName);

        var getResponse = await Client.GetAsync($"/api/characters/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<CharacterResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Polis Massa Annex", fetched.PlanetBornOnName);
        Assert.Equal("Corellian Human", fetched.SpeciesName);
    }

    [Fact]
    public async Task CreateCharacter_AsAdmin_WithEstimatedYearRange_ReturnsRange()
    {
        var client = await CreateAdminClientAsync();

        // Palpatine's birth year is only estimated: between 88 and 84 BBY. Note that -88 precedes -84.
        var createdResponse = await client.PostAsJsonAsync(
            "/api/characters", new CreateCharacterRequest("Emperor Palpatine", YearOfBirthEarliest: -88, YearOfBirthLatest: -84));

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<CharacterResponse>();

        Assert.NotNull(created);
        Assert.Null(created.PlanetBornOnId);
        Assert.Equal(-88, created.YearOfBirthEarliest);
        Assert.Equal(-84, created.YearOfBirthLatest);
    }

    [Fact]
    public async Task CreateCharacter_AsAdmin_WithInvertedYearRange_ReturnsBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/characters", new CreateCharacterRequest("Emperor Palpatine", YearOfBirthEarliest: -84, YearOfBirthLatest: -88));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCharacter_AsAdmin_UpdatesBiographyFields()
    {
        var created = await CreateCharacterAsync("Yoda");
        var species = await CreateSpeciesAsync("Shili Togruta");
        var client = await CreateAdminClientAsync();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/characters/{created.Id}",
            new UpdateCharacterRequest(YearOfBirthEarliest: -900, YearOfBirthLatest: -890, SpeciesId: species.Id));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CharacterResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Yoda", updated.Name);
        Assert.Equal(-900, updated.YearOfBirthEarliest);
        Assert.Equal(-890, updated.YearOfBirthLatest);
        Assert.Equal(species.Id, updated.SpeciesId);
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
    public async Task DeleteCharacter_WhenLinkedToEvent_ReturnsConflict()
    {
        var created = await CreateCharacterAsync("Linked character");
        var client = await CreateAdminClientAsync();
        var source = await CreateSourceMaterialAsync("Conflict Test Material");

        var eventResponse = await client.PostAsJsonAsync(
            "/api/source-material-events",
            new CreateSourceMaterialEventRequest(
                "Conflict Test Event", "desc", CanonType.Canon, 0, "0 BBY", null,
                source.Id, null, [created.Id], [], []));
        eventResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/characters/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/characters/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
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
