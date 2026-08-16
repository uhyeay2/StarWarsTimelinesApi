using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;
using StarWarsTimelines.Persistence;

namespace StarWarsTimelines.Api.Tests;

public sealed class LibraryEndpointsTests : ApiTestBase
{
    private static readonly Guid PadmeId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LukeId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PadmeEpisodeOneId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid FallenOrderId = new("00000000-0000-0000-0000-000000000022");
    private static readonly Guid CloneWarsId = new("00000000-0000-0000-0000-000000000010");
    private static readonly Guid MandalorianId = new("00000000-0000-0000-0000-000000000012");
    private static readonly Guid CloneWarsUnitOneId = new("00000000-0000-0000-0000-500000000001");
    private static readonly Guid CloneWarsUnitFiveId = new("00000000-0000-0000-0000-500000000005");
    private static readonly Guid MandalorianUnitOneId = new("00000000-0000-0000-0000-500000000006");
    private static readonly Guid FallenOrderUnitOneId = new("00000000-0000-0000-0000-500000000023");

    public LibraryEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
        ResetLibraryToSeed();
        ResetUnitProgressToSeed();
    }

    [Fact]
    public async Task GetLibrary_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"/api/users/{PadmeId}/source-materials");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLibrary_AsOwner_ReturnsSeededItems()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.GetAsync($"/api/users/{PadmeId}/source-materials");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        Assert.NotNull(items);
        Assert.Equal(7, items.Count);
        Assert.Contains(items, x => x.SourceMaterialId == PadmeEpisodeOneId && x.Status == TrackingStatus.Completed);
    }

    [Fact]
    public async Task GetLibrary_AsAnotherStandardUser_ReturnsForbidden()
    {
        var luke = await CreateClientAsAsync("luke", "luke123");

        var response = await luke.GetAsync($"/api/users/{PadmeId}/source-materials");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLibrary_AsAdmin_ReturnsUsersLibrary()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/users/{PadmeId}/source-materials");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        Assert.NotNull(items);
        Assert.Equal(7, items.Count);
    }

    [Fact]
    public async Task AddLibraryItem_AsOwner_DefaultsToWishListed()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials",
            new AddLibraryItemRequest(FallenOrderId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<LibraryItemResponse>();

        Assert.NotNull(item);
        Assert.Equal(FallenOrderId, item.SourceMaterialId);
        Assert.Equal("Star Wars Jedi: Fallen Order", item.Title);
        Assert.Equal(TrackingStatus.WishListed, item.Status);
        Assert.False(item.IsFavorite);
    }

    [Fact]
    public async Task AddLibraryItem_WhenAlreadyTracked_ReturnsExistingItemWithoutDuplicating()
    {
        var client = await CreateStandardClientAsync();

        var first = await client.PostAsJsonAsync($"/api/users/{PadmeId}/source-materials", new AddLibraryItemRequest(PadmeEpisodeOneId));
        var second = await client.PostAsJsonAsync($"/api/users/{PadmeId}/source-materials", new AddLibraryItemRequest(PadmeEpisodeOneId));

        var firstItem = await first.Content.ReadFromJsonAsync<LibraryItemResponse>();
        var secondItem = await second.Content.ReadFromJsonAsync<LibraryItemResponse>();

        Assert.NotNull(firstItem);
        Assert.NotNull(secondItem);
        Assert.Equal(PadmeEpisodeOneId, secondItem.SourceMaterialId);
        Assert.Equal(TrackingStatus.Completed, secondItem.Status);

        var library = await (await client.GetAsync($"/api/users/{PadmeId}/source-materials")).Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Single(library!, x => x.SourceMaterialId == PadmeEpisodeOneId);
    }

    [Fact]
    public async Task AddLibraryItem_WithUnknownSourceMaterial_ReturnsNotFound()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials",
            new AddLibraryItemRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLibraryItem_AsOwner_ChangesStatusAndFavorite()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{PadmeEpisodeOneId}",
            new UpdateLibraryItemRequest(TrackingStatus.InProgress, true));

        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<LibraryItemResponse>();

        Assert.NotNull(item);
        Assert.Equal(TrackingStatus.InProgress, item.Status);
        Assert.True(item.IsFavorite);
    }

    [Fact]
    public async Task UpdateLibraryItem_AsOwner_ForUntrackedSource_ReturnsNotFound()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{FallenOrderId}",
            new UpdateLibraryItemRequest(TrackingStatus.InProgress, true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLibraryItem_AsAnotherUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{LukeId}/source-materials/{PadmeEpisodeOneId}",
            new UpdateLibraryItemRequest(TrackingStatus.InProgress, true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLibraryItem_AsOwner_RemovesItem()
    {
        var client = await CreateStandardClientAsync();

        var deleteResponse = await client.DeleteAsync($"/api/users/{PadmeId}/source-materials/{PadmeEpisodeOneId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/users/{PadmeId}/source-materials");
        var items = await getResponse.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        Assert.DoesNotContain(items!, x => x.SourceMaterialId == PadmeEpisodeOneId);
    }

    [Fact]
    public async Task DeleteLibraryItem_AsAnotherUser_ReturnsForbidden()
    {
        var client = await CreateStandardClientAsync();

        var deleteResponse = await client.DeleteAsync($"/api/users/{LukeId}/source-materials/{PadmeEpisodeOneId}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetLibrary_AsOwner_IncludesUnitsWithProgress()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.GetAsync($"/api/users/{PadmeId}/source-materials");

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        var cloneWars = Assert.Single(items!, x => x.SourceMaterialId == CloneWarsId);
        Assert.Equal(5, cloneWars.Units.Count);
        Assert.Equal(Enumerable.Range(1, 5), cloneWars.Units.Select(x => x.Number));
        Assert.True(cloneWars.Units[0].IsCompleted);
        Assert.True(cloneWars.Units[1].IsCompleted);
        Assert.True(cloneWars.Units[2].IsCompleted);
        Assert.False(cloneWars.Units[3].IsCompleted);
        Assert.False(cloneWars.Units[4].IsCompleted);
    }

    [Fact]
    public async Task UpdateUnitProgress_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{CloneWarsUnitOneId}",
            new UpdateUnitProgressRequest(true));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUnitProgress_AsOwner_MarksUnitCompleted()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{CloneWarsUnitFiveId}",
            new UpdateUnitProgressRequest(true));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<LibraryUnitResponse>();

        Assert.NotNull(updated);
        Assert.Equal(CloneWarsUnitFiveId, updated.Id);
        Assert.True(updated.IsCompleted);

        var library = await (await client.GetAsync($"/api/users/{PadmeId}/source-materials")).Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        var cloneWars = Assert.Single(library!, x => x.SourceMaterialId == CloneWarsId);
        Assert.True(cloneWars.Units.Single(x => x.Id == CloneWarsUnitFiveId).IsCompleted);
    }

    [Fact]
    public async Task UpdateUnitProgress_AsOwner_UnmarksUnit()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{CloneWarsUnitOneId}",
            new UpdateUnitProgressRequest(false));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<LibraryUnitResponse>();

        Assert.NotNull(updated);
        Assert.False(updated.IsCompleted);

        var library = await (await client.GetAsync($"/api/users/{PadmeId}/source-materials")).Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        var cloneWars = Assert.Single(library!, x => x.SourceMaterialId == CloneWarsId);
        Assert.False(cloneWars.Units.Single(x => x.Id == CloneWarsUnitOneId).IsCompleted);
    }

    [Fact]
    public async Task UpdateUnitProgress_WhenUnitNotInMaterial_ReturnsNotFound()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{MandalorianUnitOneId}",
            new UpdateUnitProgressRequest(true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUnitProgress_WhenItemNotTracked_ReturnsNotFound()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{MandalorianId}/units/{MandalorianUnitOneId}",
            new UpdateUnitProgressRequest(true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUnitProgress_WhenUnitMissing_ReturnsNotFound()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{Guid.NewGuid()}",
            new UpdateUnitProgressRequest(true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUnitProgress_AsAnotherUser_ReturnsForbidden()
    {
        var luke = await CreateClientAsAsync("luke", "luke123");

        var response = await luke.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{CloneWarsUnitOneId}",
            new UpdateUnitProgressRequest(true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLibrary_AsOwner_StatusDerivedFromUnitProgress()
    {
        var client = await CreateStandardClientAsync();

        for (var number = 1; number <= 5; number++)
        {
            var unitId = new Guid($"00000000-0000-0000-0000-50000000000{number}");
            var response = await client.PutAsJsonAsync(
                $"/api/users/{PadmeId}/source-materials/{CloneWarsId}/units/{unitId}",
                new UpdateUnitProgressRequest(true));
            response.EnsureSuccessStatusCode();
        }

        var library = await (await client.GetAsync($"/api/users/{PadmeId}/source-materials")).Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        var cloneWars = Assert.Single(library!, x => x.SourceMaterialId == CloneWarsId);
        Assert.Equal(TrackingStatus.Completed, cloneWars.Status);
    }

    [Fact]
    public async Task GetLibrary_AsOwner_MaterialWithoutUnits_KeepsManualStatus()
    {
        var client = await CreateStandardClientAsync();

        var update = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{PadmeEpisodeOneId}",
            new UpdateLibraryItemRequest(TrackingStatus.InProgress, null));
        update.EnsureSuccessStatusCode();

        var library = await (await client.GetAsync($"/api/users/{PadmeId}/source-materials")).Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        var episodeOne = Assert.Single(library!, x => x.SourceMaterialId == PadmeEpisodeOneId);
        Assert.Equal(TrackingStatus.InProgress, episodeOne.Status);
        Assert.Empty(episodeOne.Units);
    }

    [Fact]
    public async Task UpdateLibraryItem_AsOwner_ForUnitBasedMaterial_WhenStatusProvided_ReturnsBadRequest()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}",
            new UpdateLibraryItemRequest(TrackingStatus.Completed, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLibraryItem_AsOwner_ForUnitBasedMaterial_FavoriteOnly_Succeeds()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/{CloneWarsId}",
            new UpdateLibraryItemRequest(null, true));

        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<LibraryItemResponse>();

        Assert.NotNull(item);
        Assert.True(item!.IsFavorite);
        Assert.Equal(TrackingStatus.InProgress, item.Status);
    }

    [Fact]
    public async Task ReorderLibrary_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/reorder",
            new ReorderLibraryItemsRequest([PadmeEpisodeOneId]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReorderLibrary_AsOwner_ReordersLibrary()
    {
        var client = await CreateStandardClientAsync();
        var desiredOrder = new[]
        {
            new Guid("00000000-0000-0000-0000-000000000021"),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("00000000-0000-0000-0000-000000000017"),
            new Guid("00000000-0000-0000-0000-000000000002"),
            new Guid("00000000-0000-0000-0000-000000000010"),
            new Guid("00000000-0000-0000-0000-000000000016"),
            new Guid("00000000-0000-0000-0000-000000000009")
        };

        var reorderResponse = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/reorder",
            new ReorderLibraryItemsRequest(desiredOrder));

        reorderResponse.EnsureSuccessStatusCode();
        var reordered = await reorderResponse.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();

        Assert.NotNull(reordered);
        Assert.Equal(desiredOrder, reordered!.Select(x => x.SourceMaterialId));

        var library = await (await client.GetAsync($"/api/users/{PadmeId}/source-materials")).Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Equal(desiredOrder, library!.Select(x => x.SourceMaterialId));
    }

    [Fact]
    public async Task ReorderLibrary_WhenListDoesNotMatchLibrary_ReturnsBadRequest()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/reorder",
            new ReorderLibraryItemsRequest([PadmeEpisodeOneId]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReorderLibrary_AsAnotherUser_ReturnsForbidden()
    {
        var luke = await CreateClientAsAsync("luke", "luke123");

        var response = await luke.PutAsJsonAsync(
            $"/api/users/{PadmeId}/source-materials/reorder",
            new ReorderLibraryItemsRequest([PadmeEpisodeOneId]));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private void ResetLibraryToSeed()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM UserSourceMaterials");

        var now = DateTime.UtcNow;
        db.UserSourceMaterials.AddRange(
            Row(PadmeId, 1, TrackingStatus.Completed, true, now),
            Row(PadmeId, 2, TrackingStatus.InProgress, false, now),
            Row(PadmeId, 10, TrackingStatus.InProgress, false, now),
            Row(PadmeId, 9, TrackingStatus.WishListed, false, now),
            Row(PadmeId, 16, TrackingStatus.Completed, true, now),
            Row(PadmeId, 17, TrackingStatus.WishListed, false, now),
            Row(PadmeId, 21, TrackingStatus.WishListed, false, now),
            Row(LukeId, 4, TrackingStatus.Completed, true, now),
            Row(LukeId, 5, TrackingStatus.Completed, true, now));
        db.SaveChanges();
    }

    private static UserSourceMaterial Row(Guid userId, int sourceSequence, TrackingStatus status, bool isFavorite, DateTime now) =>
        new()
        {
            UserId = userId,
            SourceMaterialId = new Guid($"00000000-0000-0000-0000-{sourceSequence:D12}"),
            Status = status,
            IsFavorite = isFavorite,
            CreatedAtUtc = now
        };

    private void ResetUnitProgressToSeed()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM UserSourceMaterialUnits");

        var now = DateTime.UtcNow;
        db.UserSourceMaterialUnits.AddRange(
            ProgressRow(PadmeId, CloneWarsUnitOneId, true, now),
            ProgressRow(PadmeId, new Guid("00000000-0000-0000-0000-500000000002"), true, now),
            ProgressRow(PadmeId, new Guid("00000000-0000-0000-0000-500000000003"), true, now));
        db.SaveChanges();
    }

    private static UserSourceMaterialUnit ProgressRow(Guid userId, Guid unitId, bool isCompleted, DateTime now) =>
        new()
        {
            UserId = userId,
            SourceMaterialUnitId = unitId,
            IsCompleted = isCompleted,
            UpdatedAtUtc = now
        };
}
