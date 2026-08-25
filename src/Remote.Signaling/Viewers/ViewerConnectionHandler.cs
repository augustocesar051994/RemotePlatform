using Remote.Protocol;
using Remote.Protocol.Messages;
using Remote.Protocol.Serialization;
using Remote.Signaling.Sessions;
using System.Net.WebSockets;
using System.Text.Json;

namespace Remote.Signaling.Viewers;

public sealed class ViewerConnectionHandler
{
    private readonly SessionRegistry _sessionRegistry;
    private readonly ViewerRegistry _viewerRegistry;

    public ViewerConnectionHandler(
        SessionRegistry sessionRegistry,
        ViewerRegistry viewerRegistry)
    {
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
            await context.WebSockets
                .AcceptWebSocketAsync();

        Console.WriteLine(
            "Viewer conectado via WebSocket."
        );

        var buffer = new byte[4096];

        Guid? connectedViewerId = null;

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
                    case "viewerHelloMessage":
                        {
                            var hello =
                                ProtocolSerializer
                                    .Deserialize<ViewerHelloMessage>(
                                        envelope.Payload
                                    );

                            if (hello is not null)
                            {
                                _viewerRegistry.Upsert(
                                    hello.ViewerId,
                                    webSocket
                                );

                                connectedViewerId =
                                    hello.ViewerId;

                                Console.WriteLine(
                                    $"Viewer online: {hello.ViewerId}"
                                );
                            }

                            break;
                        }
                    case "sessionConnected":
                        {
                            var connected =
                                ProtocolSerializer.Deserialize<SessionConnectedMessage>(
                                    envelope.Payload
                                );

                            if (connected is null)
                            {
                                break;
                            }

                            if (!_sessionRegistry.TryGet(
                                connected.SessionId,
                                out var session
                            ))
                            {
                                Console.WriteLine(
                                    $"Sessão não encontrada: {connected.SessionId}"
                                );

                                break;
                            }

                            if (!connectedViewerId.HasValue)
                            {
                                Console.WriteLine(
                                    "Viewer ainda não está identificado."
                                );

                                break;
                            }

                            if (session!.ViewerId != connectedViewerId.Value)
                            {
                                Console.WriteLine(
                                    $"Viewer {connectedViewerId.Value} tentou conectar uma sessão que pertence ao Viewer {session.ViewerId}."
                                );

                                break;
                            }

                            var updated =
                                _sessionRegistry.TryConnect(
                                    connected.SessionId
                                );

                            if (updated)
                            {
                                Console.WriteLine();
                                Console.WriteLine("=== Sessão conectada ===");
                                Console.WriteLine(
                                    $"Session ID: {connected.SessionId}"
                                );
                                Console.WriteLine("========================");
                            }
                            else
                            {
                                Console.WriteLine(
                                    $"Não foi possível conectar a sessão: {connected.SessionId}"
                                );
                            }

                            break;
                        }
                    default:
                        Console.WriteLine(
                            $"Tipo de mensagem do Viewer desconhecido: {envelope.Type}"
                        );
                        break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine(
                $"Viewer desconectado abruptamente: {ex.Message}"
            );
        }
        finally
        {
            if (connectedViewerId.HasValue)
            {
                _viewerRegistry.Remove(
                    connectedViewerId.Value
                );

                Console.WriteLine(
                    $"Viewer offline: {connectedViewerId.Value}"
                );
            }
        }
    }
}
