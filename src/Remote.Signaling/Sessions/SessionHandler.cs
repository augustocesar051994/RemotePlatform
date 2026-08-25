using Remote.Protocol;
using Remote.Protocol.Messages;
using Remote.Protocol.Serialization;
using Remote.Signaling.Agents;
using Remote.Signaling.Viewers;
using System.Net.WebSockets;

namespace Remote.Signaling.Sessions;

public sealed class SessionHandler
{
    private readonly AgentRegistry _agentRegistry;
    private readonly SessionRegistry _sessionRegistry;
    private readonly ViewerRegistry _viewerRegistry;

    public SessionHandler(
        AgentRegistry agentRegistry,
        SessionRegistry sessionRegistry,
        ViewerRegistry viewerRegistry)
    {
        _agentRegistry = agentRegistry;
        _sessionRegistry = sessionRegistry;
        _viewerRegistry = viewerRegistry;
    }

    public async Task<IResult> CreateAsync(
        Guid agentId,
        SessionCreateMessage message) 
    {
        var agent = _agentRegistry
        .GetAll()
        .FirstOrDefault(
            x => x.AgentId == agentId
        );

        if (agent is null)
        {
            return Results.NotFound(
                "Agent não encontrado."
            );
        }

        if (agent.WebSocket.State !=
            WebSocketState.Open)
        {
            return Results.Conflict(
                "Agent não está conectado."
            );
        }

        if (!_viewerRegistry.TryGet(
            message.ViewerId,
            out var viewer
        ))
        {
            return Results.NotFound(
                "Viewer não encontrado."
            );
        }

        if (viewer!.WebSocket.State !=
            WebSocketState.Open)
        {
            return Results.Conflict(
                "Viewer não está conectado."
            );
        }

        var sessionId = Guid.NewGuid();

        _sessionRegistry.Upsert(
            sessionId,
            agentId,
            message.ViewerId,
            SessionStatus.Requested
        );

        var request =
            new SessionRequestMessage(
                SessionId: sessionId
            );

        var sessionRequestEnvelope =
            new ProtocolEnvelope<SessionRequestMessage>(
                Type: "sessionRequest",
                Payload: request
            );

        var payload =
            ProtocolSerializer.Serialize(
                sessionRequestEnvelope
            );

        await agent.WebSocket.SendAsync(
            payload,
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None
        );

        return Results.Ok(
            new SessionCreatedMessage(
                SessionId: sessionId
            )
        );
    }
}
