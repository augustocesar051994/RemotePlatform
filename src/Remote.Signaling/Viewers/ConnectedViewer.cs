using System.Net.WebSockets;

namespace Remote.Signaling.Viewers;

public sealed record ConnectedViewer(
    Guid ViewerId,
    DateTimeOffset ConnectedAt,
    WebSocket WebSocket
);