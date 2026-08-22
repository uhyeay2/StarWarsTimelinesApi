using System.Net;
using System.Net.Http.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace StarWarsTimelines.Api.Tests;

public sealed class CatalogEndpointsTests : ApiTestBase, IClassFixture<StarWarsTimelinesApiFactory>
{
    public CatalogEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ClearCharacters();
        ClearLocations();
        ClearVehicles();
    }

    [Fact]
    public async Task CreateCharacter_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var response = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("characters", evt!.Entity);
            Assert.Equal("created", evt.Type);
            Assert.NotEqual(Guid.Empty, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task UpdateCharacter_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<CharacterResponse>())!;
            channel.Reader.TryRead(out _); // drain create event

            var response = await client.PutAsJsonAsync($"/api/characters/{created.Id}", new UpdateCharacterRequest("Luke", null, null, null, null, null, null));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("characters", evt!.Entity);
            Assert.Equal("updated", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task DeleteCharacter_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<CharacterResponse>())!;
            channel.Reader.TryRead(out _); // drain create event

            var response = await client.DeleteAsync($"/api/characters/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("characters", evt!.Entity);
            Assert.Equal("deleted", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task CreateLocation_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var response = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("Tatooine"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("locations", evt!.Entity);
            Assert.Equal("created", evt.Type);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task UpdateLocation_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("Tatooine"));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<LocationResponse>())!;
            channel.Reader.TryRead(out _);

            var response = await client.PutAsJsonAsync($"/api/locations/{created.Id}", new UpdateLocationRequest("Desert Planet"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("locations", evt!.Entity);
            Assert.Equal("updated", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task DeleteLocation_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("Tatooine"));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<LocationResponse>())!;
            channel.Reader.TryRead(out _);

            var response = await client.DeleteAsync($"/api/locations/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("locations", evt!.Entity);
            Assert.Equal("deleted", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task CreateVehicle_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var response = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("X-Wing"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("vehicles", evt!.Entity);
            Assert.Equal("created", evt.Type);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task UpdateVehicle_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("X-Wing"));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;
            channel.Reader.TryRead(out _);

            var response = await client.PutAsJsonAsync($"/api/vehicles/{created.Id}", new UpdateVehicleRequest("Starfighter"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("vehicles", evt!.Entity);
            Assert.Equal("updated", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task DeleteVehicle_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("X-Wing"));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<VehicleResponse>())!;
            channel.Reader.TryRead(out _);

            var response = await client.DeleteAsync($"/api/vehicles/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("vehicles", evt!.Entity);
            Assert.Equal("deleted", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task CreateSourceMaterial_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var response = await client.PostAsJsonAsync("/api/source-materials", new CreateSourceMaterialRequest("A New Hope", Medium.Movie, CanonType.Canon));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("source-materials", evt!.Entity);
            Assert.Equal("created", evt.Type);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task UpdateSourceMaterial_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/source-materials", new CreateSourceMaterialRequest("A New Hope", Medium.Movie, CanonType.Canon));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>())!;
            channel.Reader.TryRead(out _);

            var response = await client.PutAsJsonAsync($"/api/source-materials/{created.Id}", new UpdateSourceMaterialRequest("Star Wars: A New Hope", null, null));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("source-materials", evt!.Entity);
            Assert.Equal("updated", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task DeleteSourceMaterial_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var createResponse = await client.PostAsJsonAsync("/api/source-materials", new CreateSourceMaterialRequest("A New Hope", Medium.Movie, CanonType.Canon));
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>())!;
            channel.Reader.TryRead(out _);

            var response = await client.DeleteAsync($"/api/source-materials/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("source-materials", evt!.Entity);
            Assert.Equal("deleted", evt.Type);
            Assert.Equal(created.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task CreateSourceMaterialUnit_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var smResponse = await client.PostAsJsonAsync("/api/source-materials", new CreateSourceMaterialRequest("The Clone Wars", Medium.AnimatedShow, CanonType.Canon));
            smResponse.EnsureSuccessStatusCode();
            var sm = (await smResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>())!;
            channel.Reader.TryRead(out _); // drain SM create

            var response = await client.PostAsJsonAsync($"/api/source-materials/{sm.Id}/units", new CreateSourceMaterialUnitRequest(UnitType.Episode, null, 1, "Cat and Mouse"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("source-material-units", evt!.Entity);
            Assert.Equal("created", evt.Type);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task UpdateSourceMaterialUnit_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var smResponse = await client.PostAsJsonAsync("/api/source-materials", new CreateSourceMaterialRequest("The Clone Wars", Medium.AnimatedShow, CanonType.Canon));
            smResponse.EnsureSuccessStatusCode();
            var sm = (await smResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>())!;
            channel.Reader.TryRead(out _);

            var unitResponse = await client.PostAsJsonAsync($"/api/source-materials/{sm.Id}/units", new CreateSourceMaterialUnitRequest(UnitType.Episode, null, 1, "Cat and Mouse"));
            unitResponse.EnsureSuccessStatusCode();
            var unit = (await unitResponse.Content.ReadFromJsonAsync<SourceMaterialUnitResponse>())!;
            channel.Reader.TryRead(out _); // drain unit create

            var response = await client.PutAsJsonAsync($"/api/source-materials/{sm.Id}/units/{unit.Id}", new UpdateSourceMaterialUnitRequest(null, null, null, "Episode 1"));
            response.EnsureSuccessStatusCode();

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("source-material-units", evt!.Entity);
            Assert.Equal("updated", evt.Type);
            Assert.Equal(unit.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task DeleteSourceMaterialUnit_AsAdmin_BroadcastsEvent()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            var smResponse = await client.PostAsJsonAsync("/api/source-materials", new CreateSourceMaterialRequest("The Clone Wars", Medium.AnimatedShow, CanonType.Canon));
            smResponse.EnsureSuccessStatusCode();
            var sm = (await smResponse.Content.ReadFromJsonAsync<SourceMaterialResponse>())!;
            channel.Reader.TryRead(out _);

            var unitResponse = await client.PostAsJsonAsync($"/api/source-materials/{sm.Id}/units", new CreateSourceMaterialUnitRequest(UnitType.Episode, null, 1, "Cat and Mouse"));
            unitResponse.EnsureSuccessStatusCode();
            var unit = (await unitResponse.Content.ReadFromJsonAsync<SourceMaterialUnitResponse>())!;
            channel.Reader.TryRead(out _); // drain unit create

            var response = await client.DeleteAsync($"/api/source-materials/{sm.Id}/units/{unit.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("source-material-units", evt!.Entity);
            Assert.Equal("deleted", evt.Type);
            Assert.Equal(unit.Id, evt.Id);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }
}
