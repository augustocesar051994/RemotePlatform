using System.Collections.Concurrent;

namespace Remote.Signaling.Sessions;

public sealed class SessionRegistry
{
    private readonly ConcurrentDictionary<Guid, RemoteSession> _sessions = new();

    public void Upsert(
        Guid sessionId,
        Guid agentId,
        Guid viewerId,
        SessionStatus status)
    {
        var session = new RemoteSession(
            SessionId: sessionId,
            AgentId: agentId,
            ViewerId: viewerId,
            Status: status,
            LastUpdated: DateTimeOffset.UtcNow
        );

        _sessions[sessionId] = session;
    }

    public IReadOnlyCollection<RemoteSession> GetAll()
    {
        return _sessions.Values.ToArray();
    }

    public bool Remove(Guid sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }

    public bool TryAccept(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var existingSession))
        {
            return false;
        }

        if (existingSession.Status != SessionStatus.Requested)//It's already accepted, so I don't update
        {
            return false;
        }

        var updatedSession = existingSession with
        {
            Status = SessionStatus.Accepted,
            LastUpdated = DateTimeOffset.UtcNow
        };

        return _sessions.TryUpdate(
            sessionId,
            updatedSession,
            existingSession
        );
    }

    public bool TryConnect(Guid sessionId)
    {
        if (!_sessions.TryGetValue(
            sessionId,
            out var existingSession
        ))
        {
            return false;
        }

        if (existingSession.Status != SessionStatus.Accepted)
        {
            return false;
        }

        var updatedSession = existingSession with
        {
            Status = SessionStatus.Connected,
            LastUpdated = DateTimeOffset.UtcNow
        };

        return _sessions.TryUpdate(
            sessionId,
            updatedSession,
            existingSession
        );
    }

    public bool TryGet(
        Guid sessionId,
        out RemoteSession? session)
    {
        return _sessions.TryGetValue(
            sessionId,
            out session
        );
    }

}