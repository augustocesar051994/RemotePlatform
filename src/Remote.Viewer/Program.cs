using Remote.Protocol;
using Remote.Protocol.Messages;
using Remote.Protocol.Serialization;
using Remote.Viewer.Identity;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;

var identityProvider = new ViewerIdentityProvider();
var viewerId = identityProvider.Get();

using var webSocket = new ClientWebSocket();

var webSocketUri =
    new Uri("wss://localhost:7066/viewer/connect");

Console.WriteLine("Conectando Viewer ao Signaling...");

await webSocket.ConnectAsync(
    webSocketUri,
    CancellationToken.None
);

Console.WriteLine(
    $"Viewer conectado. Viewer ID: {viewerId}"
);

var viewerHello = new ViewerHelloMessage(
    ViewerId: viewerId
);

var viewerHelloEnvelope =
    new ProtocolEnvelope<ViewerHelloMessage>(
        Type: "viewerHelloMessage",
        Payload: viewerHello
    );

var viewerHelloPayload =
    ProtocolSerializer.Serialize(viewerHelloEnvelope);

await webSocket.SendAsync(
    viewerHelloPayload,
    WebSocketMessageType.Text,
    endOfMessage: true,
    CancellationToken.None
);

Console.WriteLine("Identificação do Viewer enviada.");

/**/

using var httpClient = new HttpClient();

var uri = "https://localhost:7066/agents";

Console.WriteLine("Consultando Agents...");

var agents = await httpClient.GetFromJsonAsync<AgentInfoMessage[]>(
    uri
);//HTTP GET + deserialize JSON → T

if (agents is null || agents.Length == 0)
{
    Console.WriteLine("Nenhum Agent disponível.");
    return;
}

Console.WriteLine();
Console.WriteLine("=== Agents disponíveis ===");

foreach (var agent in agents)
{
    Console.WriteLine($"Agent ID: {agent.AgentId}");
    Console.WriteLine($"Máquina: {agent.MachineName}");
    Console.WriteLine($"Last Seen: {agent.LastSeen}");
    Console.WriteLine();
}

var selectedAgent = agents[0];

Console.WriteLine(
    $"Solicitando sessão para: {selectedAgent.MachineName}"
);

var sessionCreate = new SessionCreateMessage(
    ViewerId: viewerId
);

var response = await httpClient.PostAsJsonAsync(
    $"https://localhost:7066/agents/{selectedAgent.AgentId}/sessions",
    sessionCreate
);

response.EnsureSuccessStatusCode();

var session =
    await response.Content.ReadFromJsonAsync<SessionCreatedMessage>();

if (session is null)
{
    Console.WriteLine("Não foi possível criar a sessão.");
    return;
}

Console.WriteLine(
    $"Sessão criada: {session.SessionId}"
);

/**/

var buffer = new byte[4096];

var sessionAccepted = false;

while (webSocket.State == WebSocketState.Open && !sessionAccepted)
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
        case "sessionAccepted":
        {
            var accepted =
                ProtocolSerializer.Deserialize<SessionAcceptedMessage>(
                    envelope.Payload
                );

            if (accepted is null)
            {
                break;
            }

            if (accepted.SessionId != session.SessionId)
            {
                Console.WriteLine(
                    $"Mensagem recebida para outra sessão: {accepted.SessionId}"
                );

                break;
            }

            sessionAccepted = true;

            Console.WriteLine();
            Console.WriteLine("=== Sessão aceita pelo Agent ===");
            Console.WriteLine(
                $"Session ID: {accepted.SessionId}"
            );
            Console.WriteLine("===============================");
                
            var sessionConnected =
                new SessionConnectedMessage(
                    SessionId: accepted.SessionId
                );

            var sessionConnectedEnvelope =
                new ProtocolEnvelope<SessionConnectedMessage>(
                    Type: "sessionConnected",
                    Payload: sessionConnected
                );

            var sessionConnectedPayload =
                ProtocolSerializer.Serialize(
                    sessionConnectedEnvelope
                );

            await webSocket.SendAsync(
                sessionConnectedPayload,
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None
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