namespace Remote.Protocol.Messages;

public sealed record SessionAcceptedMessage(
    Guid SessionId
);