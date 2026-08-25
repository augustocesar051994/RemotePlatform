namespace Remote.Protocol.Messages;

public sealed record SessionRequestMessage(
    Guid SessionId
);