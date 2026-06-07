using System.Collections.Concurrent;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public interface IAuditSink
{
    Task<AuditEventResponse> RecordAsync(AuditEventRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<(AuditEventRequest Request, AuditEventResponse Response)>> GetRecentAsync(int count, CancellationToken cancellationToken);
    Task<int> CountBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
    Task<int> DeleteBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
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

    public Task<IReadOnlyList<(AuditEventRequest Request, AuditEventResponse Response)>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var items = _events.Reverse().Take(Math.Max(1, count)).ToList();
        return Task.FromResult<IReadOnlyList<(AuditEventRequest, AuditEventResponse)>>(items);
    }

    public Task<int> CountBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_events.Count(item => item.Response.RecordedAtUtc < cutoffUtc));
    }

    public Task<int> DeleteBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var kept = new List<(AuditEventRequest Request, AuditEventResponse Response)>();
        var deleted = 0;

        while (_events.TryDequeue(out var item))
        {
            if (item.Response.RecordedAtUtc < cutoffUtc)
            {
                deleted++;
            }
            else
            {
                kept.Add(item);
            }
        }

        foreach (var item in kept)
        {
            _events.Enqueue(item);
        }

        return Task.FromResult(deleted);
    }
}
