using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelForge.Backend.Data;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

/// <summary>
/// SQLite-persisted audit sink.
/// Replaces InMemoryAuditSink for production use.
/// </summary>
public sealed class SqliteAuditSink : IAuditSink
{
    private readonly ModelForgeDbContext _db;

    public SqliteAuditSink(ModelForgeDbContext db) => _db = db;

    public async Task<AuditEventResponse> RecordAsync(AuditEventRequest request, CancellationToken ct)
    {
        var entry = new AuditEventEntry
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = request.EventType,
            ActorId = request.ActorId,
            Host = (int)request.Host,
            Severity = (int)request.Severity,
            CommandId = request.CommandId,
            ResourceId = request.ResourceId,
            MetadataJson = JsonSerializer.Serialize(request.Metadata),
            RecordedAtUtc = DateTime.UtcNow
        };

        _db.AuditEvents.Add(entry);
        await _db.SaveChangesAsync(ct);

        return new AuditEventResponse
        {
            EventId = entry.EventId,
            RecordedAtUtc = entry.RecordedAtUtc
        };
    }

    /// <summary>Query recent audit events (for admin dashboard).</summary>
    public async Task<IReadOnlyList<(AuditEventRequest Request, AuditEventResponse Response)>> GetRecentAsync(int count, CancellationToken ct)
    {
        var entries = await _db.AuditEvents
            .OrderByDescending(e => e.RecordedAtUtc)
            .Take(count)
            .ToListAsync(ct);
        return entries.Select(e => (
            new AuditEventRequest
            {
                EventType = e.EventType,
                ActorId = e.ActorId,
                Host = (OfficeHost)e.Host,
                Severity = (AuditSeverity)e.Severity,
                CommandId = e.CommandId,
                ResourceId = e.ResourceId
            },
            new AuditEventResponse
            {
                EventId = e.EventId,
                RecordedAtUtc = e.RecordedAtUtc
            }
        )).ToList();
    }

    public Task<int> CountBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        var cutoff = cutoffUtc.UtcDateTime;
        return _db.AuditEvents.CountAsync(e => e.RecordedAtUtc < cutoff, ct);
    }

    public async Task<int> DeleteBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        var cutoff = cutoffUtc.UtcDateTime;
        var deleted = await _db.AuditEvents
            .Where(e => e.RecordedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);
        return deleted;
    }
}
