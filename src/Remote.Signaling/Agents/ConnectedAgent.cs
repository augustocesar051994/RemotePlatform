using System.Net.WebSockets;

namespace Remote.Signaling.Agents;

public sealed record ConnectedAgent(
    Guid AgentId,
    string MachineName,
    DateTimeOffset LastSeen,
    WebSocket WebSocket
);