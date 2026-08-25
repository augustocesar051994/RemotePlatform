namespace Remote.Protocol;

public sealed record ProtocolEnvelope<T>(
    string Type,
    T Payload
);