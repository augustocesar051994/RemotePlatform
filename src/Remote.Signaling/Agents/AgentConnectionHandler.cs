using Remote.Protocol;
using Remote.Protocol.Messages;
using Remote.Protocol.Serialization;
using Remote.Signaling.Sessions;
using Remote.Signaling.Viewers;
using System.Net.WebSockets;
using System.Text.Json;

namespace Remote.Signaling.Agents;

public sealed class AgentConnectionHandler
{
    private readonly AgentRegistry _agentRegistry;
    private readonly SessionRegistry _sessionRegistry;
    private readonly ViewerRegistry _viewerRegistry;

    public AgentConnectionHandler(
        AgentRegistry agentRegistry,
        SessionRegistry sessionRegistry,
        ViewerRegistry viewerRegistry)
    {
        _agentRegistry = agentRegistry;
        _sessionRegistry = sessionRegistry;
        _viewerRegistry = viewerRegistry;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return;
        }

        using var webSocket =
            await context.WebSockets.AcceptWebSocketAsync();

        Console.WriteLine(
            "Agent conectado via WebSocket."
        );

        var buffer = new byte[4096];

        Guid? connectedAgentId = null;

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    buffer,
                    CancellationToken.None
                );

                if (result.MessageType ==
                    WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Conexão encerrada",
                        CancellationToken.None
                    );

                    break;
                }

                var envelope =
                    ProtocolSerializer.Deserialize<
                        ProtocolEnvelope<JsonElement>
                    >(
                        buffer.AsSpan(0, result.Count)
                    );

                if (envelope is null)
                {
                    continue;
                }

                switch (envelope.Type)
                {
                    case "helloMessage":
                        {
                            var hello =
                                ProtocolSerializer.Deserialize<HelloMessage>(
                                    envelope.Payload
                                );

                            if (hello is not null)
                            {
                                _agentRegistry.Upsert(
                                    hello.AgentId,
                                    hello.MachineName,
                                    webSocket
                                );

                                connectedAgentId =
                                    hello.AgentId;

                                Console.WriteLine(
                                    $"Agent online: {hello.AgentId}"
                                );
                            }

                            break;
                        }

                    case "sessionAccepted":
                        {
                            var accepted =
                                ProtocolSerializer
                                    .Deserialize<SessionAcceptedMessage>(
                                        envelope.Payload
                                    );

                            if (accepted is null ||
                                !connectedAgentId.HasValue)
                            {
                                break;
                            }

                            var updated =
                                _sessionRegistry.TryAccept(
                                    accepted.SessionId
                                );

                            if (!updated)
                            {
                                Console.WriteLine(
                                    $"Não foi possível aceitar a sessão: {accepted.SessionId}"
                                );

                                break;
                            }

                            if (!_sessionRegistry.TryGet(
                                accepted.SessionId,
                                out var session
                            ))
                            {
                                Console.WriteLine(
                                    $"Sessão não encontrada: {accepted.SessionId}"
                                );

                                break;
                            }

                            if (!_viewerRegistry.TryGet(
                                session!.ViewerId,
                                out var viewer
                            ))
                            {
                                Console.WriteLine(
                                    $"Viewer não encontrado: {session.ViewerId}"
                                );

                                break;
                            }

                            if (viewer!.WebSocket.State !=
                                WebSocketState.Open)
                            {
                                Console.WriteLine(
                                    $"Viewer não está conectado: {session.ViewerId}"
                                );

                                break;
                            }

                            var sessionAcceptedEnvelope =
                                new ProtocolEnvelope<SessionAcceptedMessage>(
                                    Type: "sessionAccepted",
                                    Payload: accepted
                                );

                            var sessionAcceptedPayload =
                                ProtocolSerializer.Serialize(
                                    sessionAcceptedEnvelope
                                );

                            await viewer.WebSocket.SendAsync(
                                sessionAcceptedPayload,
                                WebSocketMessageType.Text,
                                endOfMessage: true,
                                CancellationToken.None
                            );

                            Console.WriteLine(
                                "=== Sessão aceita ==="
                            );

                            Console.WriteLine(
                                $"Session ID: {accepted.SessionId}"
                            );

                            break;
                        }

                    default:
                        Console.WriteLine(
                            $"Tipo de mensagem desconhecido: {envelope.Type}"
                        );
                        break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine(
                $"WebSocket desconectado abruptamente: {ex.Message}"
            );
        }
        finally
        {
            if (connectedAgentId.HasValue)
            {
                _agentRegistry.Remove(
                    connectedAgentId.Value
                );

                Console.WriteLine(
                    $"Agent offline: {connectedAgentId.Value}"
                );
            }
        }
    }
}