using Remote.Agent.Identity;
using Remote.Agent.Screen;
using Remote.Protocol;
using Remote.Protocol.Messages;
using Remote.Protocol.Serialization;
using System.Net.WebSockets;
using System.Text.Json;

using var webSocket = new ClientWebSocket();

var uri = new Uri("wss://localhost:7066/agent/connect");

Console.WriteLine("Conectando ao Signaling...");

await webSocket.ConnectAsync(
    uri,
    CancellationToken.None
);

Console.WriteLine($"WebSocket conectado. Estado: {webSocket.State}");

var identityProvider = new AgentIdentityProvider();
var agentId = identityProvider.GetOrCreate();

var hello = new HelloMessage(
    ProtocolVersion: 1,
    AgentId: agentId,
    MachineName: Environment.MachineName
);

var helloEnvelope = new ProtocolEnvelope<HelloMessage>(
    Type: "helloMessage",
    Payload: hello
);

var helloPayload =
    ProtocolSerializer.Serialize(helloEnvelope);

await webSocket.SendAsync(
    helloPayload,
    WebSocketMessageType.Text,
    endOfMessage: true,
    CancellationToken.None
);

Console.WriteLine(
    $"Identificação enviada. Agent ID: {agentId}"
);

var buffer = new byte[4096];

while (webSocket.State == WebSocketState.Open)
{
    var result = await webSocket.ReceiveAsync(
        buffer,
        CancellationToken.None
    );

    if (result.MessageType == WebSocketMessageType.Close)
    {
        break;
    }

    var envelope =
        ProtocolSerializer.Deserialize<ProtocolEnvelope<JsonElement>>(
            buffer.AsSpan(0, result.Count)
        );

    if (envelope is null)
    {
        continue;
    }

    switch (envelope.Type)
    {
        case "sessionRequest":
            {
                var sessionRequest =
                    ProtocolSerializer.Deserialize<SessionRequestMessage>(
                        envelope.Payload
                    );

                if (sessionRequest is null)
                {
                    break;
                }

                Console.WriteLine();
                Console.WriteLine("=== Nova solicitação de sessão ===");
                Console.WriteLine(
                    $"Session ID: {sessionRequest.SessionId}"
                );
                Console.WriteLine("=================================");
                Console.WriteLine();

                var sessionAccepted =
                    new SessionAcceptedMessage(
                        SessionId: sessionRequest.SessionId
                    );

                var sessionAcceptedEnvelope =
                    new ProtocolEnvelope<SessionAcceptedMessage>(
                        Type: "sessionAccepted",
                        Payload: sessionAccepted
                    );

                var sessionAcceptedPayload =
                    ProtocolSerializer.Serialize(
                        sessionAcceptedEnvelope
                    );

                await webSocket.SendAsync(
                    sessionAcceptedPayload,
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None
                );

                Console.WriteLine(
                    $"Sessão aceita: {sessionRequest.SessionId}"
                );

                var screenCapture = 
                    new ScreenCapture();

                var frame =
                    screenCapture.Capture();

                Console.WriteLine(
                    $"Frame capturado: {frame.Length:N0} bytes"
                );

                await File.WriteAllBytesAsync(
                    "capture.jpg",
                    frame
                );

                Console.WriteLine(
                    Path.GetFullPath("capture.jpg")
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

await webSocket.CloseAsync(
    WebSocketCloseStatus.NormalClosure,
    "Encerramento solicitado pelo Agent",
    CancellationToken.None
);

Console.WriteLine("WebSocket encerrado.");