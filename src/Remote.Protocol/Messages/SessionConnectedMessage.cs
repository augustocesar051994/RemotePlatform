namespace Remote.Protocol.Messages;

public sealed record SessionConnectedMessage(
    Guid SessionId
);