using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Remote.Signaling.Agents;

public sealed class AgentRegistry
{
    private readonly ConcurrentDictionary<Guid, ConnectedAgent> _agents = new();

    public void Upsert(Guid agentId, string machineName, WebSocket webSocket)
    {
        var agent = new ConnectedAgent(
            AgentId: agentId,
            MachineName: machineName,
            LastSeen: DateTimeOffset.UtcNow,
            WebSocket: webSocket
        );

        _agents[agentId] = agent;
    }

    public IReadOnlyCollection<ConnectedAgent> GetAll()
    {
        return _agents.Values.ToArray();
    }

    public bool Remove(Guid agentId) 
    {
        return _agents.TryRemove(agentId, out _);
    }
}