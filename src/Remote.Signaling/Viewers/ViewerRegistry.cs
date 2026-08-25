using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Remote.Signaling.Viewers;

public sealed class ViewerRegistry
{
    private readonly ConcurrentDictionary<Guid, ConnectedViewer> _viewers = new();

    public void Upsert(
        Guid viewerId,
        WebSocket webSocket)
    {
        var viewer = new ConnectedViewer(
            ViewerId: viewerId,
            ConnectedAt: DateTimeOffset.UtcNow,
            WebSocket: webSocket
        );

        _viewers[viewerId] = viewer;
    }

    public bool TryGet(
        Guid viewerId,
        out ConnectedViewer? viewer)
    {
        return _viewers.TryGetValue(
            viewerId,
            out viewer
        );
    }

    public bool Remove(Guid viewerId)
    {
        return _viewers.TryRemove(viewerId, out _);
    }
}