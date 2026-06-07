using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests.Services;

public class AuditSinkTests
{
    [Fact]
    public async Task RecordAsync_ReturnsResponseWithEventId()
    {
        var sink = new InMemoryAuditSink();
        var request = new AuditEventRequest
        {
            EventType = "command.executed",
            ActorId = "user-1",
            Host = OfficeHost.Excel,
            CommandId = "excel.fill-down"
        };

        var result = await sink.RecordAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.EventId);
    }

    [Fact]
    public async Task RecordAsync_ReturnsTimelyResponse()
    {
        var sink = new InMemoryAuditSink();
        var before = DateTimeOffset.UtcNow;
        var request = new AuditEventRequest { EventType = "login", ActorId = "user-1" };

        var result = await sink.RecordAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.RecordedAtUtc >= before);
        Assert.True(result.RecordedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task RecordAsync_SupportsCancellation()
    {
        var sink = new InMemoryAuditSink();
        var request = new AuditEventRequest { EventType = "test", ActorId = "user-1" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sink.RecordAsync(request, cts.Token));
    }

    [Fact]
    public async Task DeleteBeforeAsync_RemovesMatchingEvents()
    {
        var sink = new InMemoryAuditSink();
        await sink.RecordAsync(new AuditEventRequest { EventType = "old", ActorId = "user-1" }, CancellationToken.None);
        await sink.RecordAsync(new AuditEventRequest { EventType = "new", ActorId = "user-1" }, CancellationToken.None);

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(1);
        var matched = await sink.CountBeforeAsync(cutoff, CancellationToken.None);
        var deleted = await sink.DeleteBeforeAsync(cutoff, CancellationToken.None);
        var remaining = await sink.GetRecentAsync(10, CancellationToken.None);

        Assert.Equal(2, matched);
        Assert.Equal(2, deleted);
        Assert.Empty(remaining);
    }
}
