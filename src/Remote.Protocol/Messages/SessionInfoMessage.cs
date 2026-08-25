namespace Remote.Protocol.Messages;

public sealed record SessionInfoMessage(
    Guid SessionId,
    Guid AgentId,
    string Status,
    DateTimeOffset LastUpdated
);