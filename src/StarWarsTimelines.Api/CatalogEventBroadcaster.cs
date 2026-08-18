using System.Collections.Concurrent;
using System.Threading.Channels;

namespace StarWarsTimelines.Api;

/// <summary>
/// Represents a catalog change event broadcast to connected SSE clients.
/// </summary>
/// <param name="Entity">The catalog entity type that changed (e.g. <c>characters</c>, <c>source-materials</c>).</param>
/// <param name="Type">The mutation type: <c>created</c>, <c>updated</c>, or <c>deleted</c>.</param>
/// <param name="Id">The identifier of the affected entity, when applicable.</param>
public sealed record CatalogEvent(string Entity, string Type, Guid? Id = null);

/// <summary>
/// Manages server-sent event (SSE) connections for catalog change notifications.
///
/// Each connected client subscribes via <see cref="Subscribe"/>, which returns a
/// unique identifier and an unbounded channel. The SSE endpoint reads from the
/// channel and streams events to the client. When a catalog mutation occurs, the
/// endpoint calls <see cref="BroadcastAsync"/> to push the event to all subscribers.
/// </summary>
public sealed class CatalogEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<CatalogEvent>> _channels = new();

    /// <summary>
    /// Registers a new SSE subscriber and returns a unique identifier together
    /// with the channel the endpoint should read from.
    /// </summary>
    /// <returns>A tuple of the subscription identifier and the event channel.</returns>
    public (Guid Id, Channel<CatalogEvent> Channel) Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<CatalogEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        _channels.TryAdd(id, channel);
        return (id, channel);
    }

    /// <summary>
    /// Removes a subscriber so it no longer receives broadcast events.
    /// </summary>
    /// <param name="id">The subscription identifier returned by <see cref="Subscribe"/>.</param>
    public void Unsubscribe(Guid id)
    {
        _channels.TryRemove(id, out _);
    }

    /// <summary>
    /// Pushes a catalog event to every connected subscriber.
    /// Dead or full channels are silently skipped.
    /// </summary>
    /// <param name="evt">The catalog event to broadcast.</param>
    public async Task BroadcastAsync(CatalogEvent evt)
    {
        foreach (var kvp in _channels)
        {
            try
            {
                await kvp.Value.Writer.WriteAsync(evt);
            }
            catch (ChannelClosedException)
            {
                // Channel was closed by the client disconnecting; remove it.
                _channels.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <summary>
    /// Returns the number of active subscribers. Intended for diagnostics and testing.
    /// </summary>
    public int SubscriberCount => _channels.Count;
}
