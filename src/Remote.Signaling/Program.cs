using Remote.Protocol;
using Remote.Protocol.Messages;
using Remote.Protocol.Serialization;
using Remote.Signaling.Agents;
using Remote.Signaling.Sessions;
using Remote.Signaling.Viewers;
using System.Net.WebSockets;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AgentRegistry>();
builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddSingleton<ViewerRegistry>();

builder.Services.AddSingleton<AgentConnectionHandler>();
builder.Services.AddSingleton<SessionHandler>();
builder.Services.AddSingleton<ViewerConnectionHandler>();

var app = builder.Build();

app.UseWebSockets();

app.UseHttpsRedirection();

app.MapGet("/", () => "Remote Signaling OK");

app.MapPost("/agent/hello", (HelloMessage hello) =>
{
    return Results.Ok(new
    {
        accepted = true,
        serverTime = DateTimeOffset.UtcNow
    });
});

app.MapGet("/agents", (AgentRegistry registry) =>
{
    var agents = registry
        .GetAll()
        .Select(agent => new AgentInfoMessage(
            AgentId: agent.AgentId,
            MachineName: agent.MachineName,
            LastSeen: agent.LastSeen
        ))
        .ToArray();

    return Results.Ok(agents);
});

app.Map("/agent/connect", async (
    HttpContext context,
    AgentConnectionHandler handler) =>
{
    await handler.HandleAsync(context);
});

app.Map("/viewer/connect", async (
    HttpContext context,
    ViewerConnectionHandler handler) =>
{
    await handler.HandleAsync(context);
});

app.MapPost("/agents/{agentId:guid}/message", async (
    Guid agentId,
    ProtocolEnvelope<JsonElement> message,
    AgentRegistry registry) =>
{
    Console.WriteLine(
        $"POST message recebido para: {agentId}"
    );

    var agent = registry
        .GetAll()
        .FirstOrDefault(
            x => x.AgentId == agentId
        );

    if (agent is null)
    {
        return Results.NotFound();
    }

    if (agent.WebSocket.State !=
        WebSocketState.Open)
    {
        return Results.Conflict(
            "Agent não está conectado."
        );
    }

    var payload =
        ProtocolSerializer.Serialize(message);

    Console.WriteLine(
        $"Agent encontrado. Socket: {agent.WebSocket.State}"
    );

    await agent.WebSocket.SendAsync(
        payload,
        WebSocketMessageType.Text,
        endOfMessage: true,
        CancellationToken.None
    );

    return Results.Ok();
});

app.MapPost("/agents/{agentId:guid}/sessions", async (
    Guid agentId,
    SessionCreateMessage message,
    SessionHandler handler) =>
{
    return await handler.CreateAsync(
        agentId, 
        message
    );
});

app.MapGet("/sessions", (
    SessionRegistry registry) =>
{
    return Results.Ok(
        registry.GetAll()
    );
});

app.MapGet("/sessions/{sessionId:guid}", (
    Guid sessionId,
    SessionRegistry sessionRegistry) =>
{
    if (!sessionRegistry.TryGet(
        sessionId,
        out var session
    ))
    {
        return Results.NotFound();
    }

    var response =
        new SessionInfoMessage(
            SessionId: session!.SessionId,
            AgentId: session.AgentId,
            Status: session.Status.ToString(),
            LastUpdated: session.LastUpdated
        );

    return Results.Ok(response);
});

app.Run();