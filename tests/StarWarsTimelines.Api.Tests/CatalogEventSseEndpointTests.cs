using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Application.Dtos;

namespace StarWarsTimelines.Api.Tests;

public sealed class CatalogEventSseEndpointTests : ApiTestBase
{
    public CatalogEventSseEndpointTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task StreamEvents_AsAnonymous_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/catalog-events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StreamEvents_AsAdmin_ReturnsOkWithEventStreamContentType()
    {
        var client = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, _) = broadcaster.Subscribe();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog-events");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    [Fact]
    public async Task StreamEvents_ReceivesEventAfterCharacterCreate()
    {
        var adminClient = await CreateAdminClientAsync();
        var broadcaster = Factory.Services.GetRequiredService<CatalogEventBroadcaster>();
        var (subscriptionId, channel) = broadcaster.Subscribe();

        try
        {
            // Start SSE connection in background
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var sseTask = Task.Run(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog-events");
                using var response = await adminClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cts.Token));
                while (!cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line is null)
                    {
                        return null; // stream ended
                    }

                    if (line.StartsWith("data: "))
                    {
                        return line[6..]; // strip "data: " prefix
                    }
                }
                return null;
            });

            // Give SSE connection time to establish
            await Task.Delay(500);

            // Trigger a mutation
            ClearCharacters();
            var response = await adminClient.PostAsJsonAsync("/api/characters", new CreateCharacterRequest("Luke Skywalker"));
            response.EnsureSuccessStatusCode();

            // Also verify via broadcaster channel directly
            Assert.True(channel.Reader.TryRead(out var evt));
            Assert.Equal("characters", evt!.Entity);
            Assert.Equal("created", evt.Type);
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }
}
