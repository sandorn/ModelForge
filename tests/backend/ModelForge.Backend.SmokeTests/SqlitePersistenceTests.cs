using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModelForge.Backend.Data;
using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests.Persistence;

/// <summary>
/// Tests for SQLite-backed persistent services using an in-memory SQLite database.
/// </summary>
public class SqlitePersistenceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly ModelForgeDbContext _db;

    public SqlitePersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ModelForgeDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new ModelForgeDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    // ── Configuration Store ──

    [Fact]
    public async Task SqliteConfigurationStore_UpsertAndGet()
    {
        var store = new SqliteConfigurationStore(_db);

        await store.UpsertAsync("test", new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["key1"] = "value1" },
            UpdatedBy = "tester"
        }, CancellationToken.None);

        var result = await store.GetAsync("test", CancellationToken.None);
        Assert.Equal("test", result.Scope);
        Assert.Equal("value1", result.Values["key1"]);
    }

    [Fact]
    public async Task SqliteConfigurationStore_OverwritesExisting()
    {
        var store = new SqliteConfigurationStore(_db);

        await store.UpsertAsync("s", new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["k"] = "old" }
        }, CancellationToken.None);

        await store.UpsertAsync("s", new ConfigurationUpsertRequest
        {
            Values = new Dictionary<string, string> { ["k"] = "new" }
        }, CancellationToken.None);

        var result = await store.GetAsync("s", CancellationToken.None);
        Assert.Equal("new", result.Values["k"]);
    }

    [Fact]
    public async Task SqliteConfigurationStore_EmptyScope()
    {
        var store = new SqliteConfigurationStore(_db);
        var result = await store.GetAsync("nonexistent", CancellationToken.None);
        Assert.Equal("nonexistent", result.Scope);
        Assert.Empty(result.Values);
    }

    // ── Audit Sink ──

    [Fact]
    public async Task SqliteAuditSink_RecordsEvent()
    {
        var sink = new SqliteAuditSink(_db);
        var request = new AuditEventRequest
        {
            EventType = "test.event",
            ActorId = "tester",
            Host = OfficeHost.Excel,
            Severity = AuditSeverity.Information
        };

        var result = await sink.RecordAsync(request, CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.EventId);
    }

    [Fact]
    public async Task SqliteAuditSink_GetRecentEvents()
    {
        var sink = new SqliteAuditSink(_db);

        for (int i = 0; i < 5; i++)
        {
            await sink.RecordAsync(new AuditEventRequest
            {
                EventType = $"test.event.{i}",
                ActorId = "tester"
            }, CancellationToken.None);
        }

        var recent = await sink.GetRecentAsync(3, CancellationToken.None);
        Assert.Equal(3, recent.Count);
        Assert.True(recent[0].Response.RecordedAtUtc >= recent[1].Response.RecordedAtUtc);
    }

    [Fact]
    public async Task SqliteAuditSink_DeleteBeforeRemovesOnlyExpiredEvents()
    {
        var sink = new SqliteAuditSink(_db);
        _db.AuditEvents.AddRange(
            new AuditEventEntry
            {
                EventId = "old-event",
                EventType = "audit.old",
                ActorId = "tester",
                Host = (int)OfficeHost.Web,
                Severity = (int)AuditSeverity.Information,
                RecordedAtUtc = DateTime.UtcNow.AddDays(-120)
            },
            new AuditEventEntry
            {
                EventId = "new-event",
                EventType = "audit.new",
                ActorId = "tester",
                Host = (int)OfficeHost.Web,
                Severity = (int)AuditSeverity.Information,
                RecordedAtUtc = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-90);
        var matched = await sink.CountBeforeAsync(cutoff, CancellationToken.None);
        var deleted = await sink.DeleteBeforeAsync(cutoff, CancellationToken.None);
        var recent = await sink.GetRecentAsync(10, CancellationToken.None);

        Assert.Equal(1, matched);
        Assert.Equal(1, deleted);
        Assert.DoesNotContain(recent, item => item.Response.EventId == "old-event");
        Assert.Contains(recent, item => item.Response.EventId == "new-event");
    }
    // ── Link Metadata Store ──

    [Fact]
    public async Task SqliteLinkMetadataStore_CreateAndGet()
    {
        var store = new SqliteLinkMetadataStore(_db);
        var request = new CreateLinkMetadataRequest
        {
            SourceType = LinkSourceType.ExcelRange,
            SourceDocumentId = "wb-1",
            SourceAddress = "Sheet1!A1:C10",
            TargetType = LinkTargetType.PowerPointShape,
            TargetDocumentId = "deck-1",
            TargetAddress = "Slide2/Shape3"
        };

        var created = await store.CreateAsync(request, CancellationToken.None);
        Assert.NotEmpty(created.LinkId);
        Assert.Equal("wb-1", created.SourceDocumentId);

        var all = await store.GetAllAsync(CancellationToken.None);
        Assert.Single(all);
    }

    [Fact]
    public async Task SqliteLinkMetadataStore_RefreshExistingLink()
    {
        var store = new SqliteLinkMetadataStore(_db);
        var created = await store.CreateAsync(new CreateLinkMetadataRequest
        {
            SourceType = LinkSourceType.ExcelChart,
            SourceDocumentId = "wb-1",
            SourceAddress = "Chart1",
            TargetType = LinkTargetType.PowerPointChart,
            TargetDocumentId = "deck-1",
            TargetAddress = "Slide3/Chart2"
        }, CancellationToken.None);

        var refreshResult = await store.MarkRefreshRequestedAsync(new LinkRefreshRequest
        {
            LinkId = created.LinkId,
            RequestedBy = "tester"
        }, CancellationToken.None);

        Assert.Equal(CommandStatus.Accepted, refreshResult.Status);
    }

    [Fact]
    public async Task SqliteLinkMetadataStore_RefreshNonExistentLink()
    {
        var store = new SqliteLinkMetadataStore(_db);
        var result = await store.MarkRefreshRequestedAsync(new LinkRefreshRequest
        {
            LinkId = "nonexistent",
            RequestedBy = "tester"
        }, CancellationToken.None);

        Assert.Equal(CommandStatus.Failed, result.Status);
    }
    [Fact]
    public async Task SqliteAuditSink_GetRecentRespectsCount()
    {
        var sink = new SqliteAuditSink(_db);
        for (int i = 0; i < 10; i++)
        {
            await sink.RecordAsync(new AuditEventRequest
            {
                EventType = "test",
                ActorId = "tester"
            }, CancellationToken.None);
        }

        var recent = await sink.GetRecentAsync(5, CancellationToken.None);
        Assert.Equal(5, recent.Count);
    }

    [Fact]
    public async Task SqliteAuditSink_EmptyReturnsEmpty()
    {
        // Use a fresh connection to ensure empty state
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ModelForge.Backend.Data.ModelForgeDbContext>()
            .UseSqlite(conn)
            .Options;
        using var db = new ModelForge.Backend.Data.ModelForgeDbContext(opts);
        await db.Database.EnsureCreatedAsync();

        var sink = new SqliteAuditSink(db);
        var recent = await sink.GetRecentAsync(10, CancellationToken.None);
        Assert.Empty(recent);
    }
}
