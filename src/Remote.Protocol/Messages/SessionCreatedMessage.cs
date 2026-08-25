namespace Remote.Protocol.Messages;

public sealed record SessionCreatedMessage(
    Guid SessionId
);