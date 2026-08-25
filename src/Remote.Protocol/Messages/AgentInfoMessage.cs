namespace Remote.Protocol.Messages;

public sealed record AgentInfoMessage(
    Guid AgentId,
    string MachineName,
    DateTimeOffset LastSeen
);