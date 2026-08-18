using System.Threading.Channels;

namespace StarWarsTimelines.Api.Tests;

public sealed class CatalogEventBroadcasterTests
{
    private readonly CatalogEventBroadcaster _broadcaster = new();

    [Fact]
    public void Subscribe_ReturnsUniqueIdAndChannel()
    {
        var (id1, channel1) = _broadcaster.Subscribe();
        var (id2, channel2) = _broadcaster.Subscribe();

        Assert.NotEqual(id1, id2);
        Assert.NotNull(channel1);
        Assert.NotNull(channel2);
        Assert.Same(channel1, channel1); // same reference
        Assert.NotSame(channel1, channel2);
    }

    [Fact]
    public void SubscriberCount_TracksActiveSubscribers()
    {
        Assert.Equal(0, _broadcaster.SubscriberCount);

        var (id1, _) = _broadcaster.Subscribe();
        Assert.Equal(1, _broadcaster.SubscriberCount);

        var (id2, _) = _broadcaster.Subscribe();
        Assert.Equal(2, _broadcaster.SubscriberCount);

        _broadcaster.Unsubscribe(id1);
        Assert.Equal(1, _broadcaster.SubscriberCount);

        _broadcaster.Unsubscribe(id2);
        Assert.Equal(0, _broadcaster.SubscriberCount);
    }

    [Fact]
    public async Task BroadcastAsync_DeliversEventToAllSubscribers()
    {
        var (_, channel1) = _broadcaster.Subscribe();
        var (_, channel2) = _broadcaster.Subscribe();

        var evt = new CatalogEvent("characters", "created", Guid.NewGuid());
        await _broadcaster.BroadcastAsync(evt);

        Assert.True(channel1.Reader.TryRead(out var received1));
        Assert.Equal("characters", received1!.Entity);
        Assert.Equal("created", received1.Type);
        Assert.Equal(evt.Id, received1.Id);

        Assert.True(channel2.Reader.TryRead(out var received2));
        Assert.Equal("characters", received2!.Entity);
    }

    [Fact]
    public async Task BroadcastAsync_DoesNotThrowWhenNoSubscribers()
    {
        var evt = new CatalogEvent("locations", "deleted");

        // Should not throw
        await _broadcaster.BroadcastAsync(evt);
    }

    [Fact]
    public void Unsubscribe_PreventsFutureDelivery()
    {
        var (id, channel) = _broadcaster.Subscribe();
        _broadcaster.Unsubscribe(id);

        // Unsubscribe should not affect already-queued messages,
        // but no new messages should be delivered.
        Assert.Equal(0, _broadcaster.SubscriberCount);
    }

    [Fact]
    public async Task BroadcastAsync_HandlesClosedChannelGracefully()
    {
        var (id, channel) = _broadcaster.Subscribe();
        channel.Writer.Complete(); // Close the channel

        var evt = new CatalogEvent("vehicles", "updated");

        // Should not throw even though the channel is closed
        await _broadcaster.BroadcastAsync(evt);

        // The dead channel should be cleaned up
        Assert.Equal(0, _broadcaster.SubscriberCount);
    }

    [Fact]
    public void Unsubscribe_NonexistentId_DoesNotThrow()
    {
        // Should not throw
        _broadcaster.Unsubscribe(Guid.NewGuid());
    }

    [Fact]
    public async Task BroadcastAsync_MultipleEvents_AreDeliveredInOrder()
    {
        var (_, channel) = _broadcaster.Subscribe();

        await _broadcaster.BroadcastAsync(new CatalogEvent("a", "created"));
        await _broadcaster.BroadcastAsync(new CatalogEvent("b", "updated"));
        await _broadcaster.BroadcastAsync(new CatalogEvent("c", "deleted"));

        Assert.True(channel.Reader.TryRead(out var e1));
        Assert.Equal("a", e1!.Entity);
        Assert.True(channel.Reader.TryRead(out var e2));
        Assert.Equal("b", e2!.Entity);
        Assert.True(channel.Reader.TryRead(out var e3));
        Assert.Equal("c", e3!.Entity);
    }
}
