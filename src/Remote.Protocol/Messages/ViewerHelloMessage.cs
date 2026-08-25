namespace Remote.Protocol.Messages;

public sealed record ViewerHelloMessage(
    Guid ViewerId
);