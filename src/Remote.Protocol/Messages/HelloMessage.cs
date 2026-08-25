namespace Remote.Protocol.Messages;

public sealed record HelloMessage(
    int ProtocolVersion,
    Guid AgentId,
    string MachineName
);