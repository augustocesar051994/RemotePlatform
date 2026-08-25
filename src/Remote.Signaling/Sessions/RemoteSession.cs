namespace Remote.Signaling.Sessions;

public sealed record RemoteSession(
    Guid SessionId,
    Guid AgentId,
    Guid ViewerId,
    SessionStatus Status,
    DateTimeOffset LastUpdated
);