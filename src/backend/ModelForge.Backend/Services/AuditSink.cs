using System.Collections.Concurrent;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public interface IAuditSink
{
    Task<AuditEventResponse> RecordAsync(AuditEventRequest request, CancellationToken cancellationToken);
}

public sealed class InMemoryAuditSink : IAuditSink
{
    private readonly ConcurrentQueue<(AuditEventRequest Request, AuditEventResponse Response)> _events = new();

    public Task<AuditEventResponse> RecordAsync(AuditEventRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new AuditEventResponse
        {
            EventId = Guid.NewGuid().ToString("N"),
            RecordedAtUtc = DateTimeOffset.UtcNow
        };

        _events.Enqueue((request, response));
        return Task.FromResult(response);
    }
}
