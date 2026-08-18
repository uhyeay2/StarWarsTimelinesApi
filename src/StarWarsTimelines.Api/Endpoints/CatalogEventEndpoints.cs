using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace StarWarsTimelines.Api.Endpoints;

/// <summary>
/// Maps the server-sent events (SSE) endpoint for catalog change notifications.
/// </summary>
public static class CatalogEventEndpoints
{
    /// <summary>
    /// Registers the SSE endpoint under <c>api/catalog-events</c>.
    /// </summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The created route group.</returns>
    public static RouteGroupBuilder MapCatalogEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/catalog-events").WithTags("Catalog Events");

        // Streams catalog change events as server-sent events.
        // Any authenticated user can subscribe — invalidation events are not sensitive.
        group.MapGet("/", StreamEvents)
            .WithName("StreamCatalogEvents")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task StreamEvents(
        HttpContext context,
        CatalogEventBroadcaster broadcaster,
        CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        var (id, channel) = broadcaster.Subscribe();
        try
        {
            var reader = channel.Reader;
            while (!ct.IsCancellationRequested && !context.RequestAborted.IsCancellationRequested)
            {
                var cancellationToken = CancellationTokenSource
                    .CreateLinkedTokenSource(ct, context.RequestAborted).Token;

                if (await reader.WaitToReadAsync(cancellationToken))
                {
                    while (reader.TryRead(out var evt))
                    {
                        var json = JsonSerializer.Serialize(evt, JsonSerializerOptions.Web);
                        await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or host shutting down — expected.
        }
        finally
        {
            broadcaster.Unsubscribe(id);
        }
    }
}
