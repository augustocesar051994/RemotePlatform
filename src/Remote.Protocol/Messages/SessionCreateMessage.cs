namespace Remote.Protocol.Messages;

public sealed record SessionCreateMessage(
    Guid ViewerId
);