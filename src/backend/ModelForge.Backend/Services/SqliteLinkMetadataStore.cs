using Microsoft.EntityFrameworkCore;
using ModelForge.Backend.Data;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

/// <summary>
/// SQLite-persisted link metadata store.
/// Replaces InMemoryLinkMetadataStore for production use.
/// </summary>
public sealed class SqliteLinkMetadataStore : ILinkMetadataStore
{
    private readonly ModelForgeDbContext _db;

    public SqliteLinkMetadataStore(ModelForgeDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<LinkMetadata>> GetAllAsync(CancellationToken ct)
    {
        var entries = await _db.LinkMetadata.ToListAsync(ct);
        return entries.Select(Map).ToArray();
    }

    public async Task<LinkMetadata> CreateAsync(CreateLinkMetadataRequest request, CancellationToken ct)
    {
        var entry = new LinkMetadataEntry
        {
            LinkId = Guid.NewGuid().ToString("N"),
            SourceType = request.SourceType,
            SourceDocumentId = request.SourceDocumentId,
            SourceAddress = request.SourceAddress,
            TargetType = request.TargetType,
            TargetDocumentId = request.TargetDocumentId,
            TargetAddress = request.TargetAddress,
            RefreshPolicy = string.IsNullOrWhiteSpace(request.RefreshPolicy) ? "manual" : request.RefreshPolicy,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.LinkMetadata.Add(entry);
        await _db.SaveChangesAsync(ct);
        return Map(entry);
    }

    public async Task<LinkRefreshResponse> MarkRefreshRequestedAsync(LinkRefreshRequest request, CancellationToken ct)
    {
        var entry = await _db.LinkMetadata.FindAsync([request.LinkId], ct);
        if (entry == null)
        {
            return new LinkRefreshResponse
            {
                LinkId = request.LinkId,
                Status = CommandStatus.Failed,
                Message = "Link metadata not found."
            };
        }

        entry.LastRefreshedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new LinkRefreshResponse
        {
            LinkId = request.LinkId,
            Status = CommandStatus.Accepted,
            Message = "Refresh request recorded. Actual Office object refresh happens on the Sidecar execution side."
        };
    }

    private static LinkMetadata Map(LinkMetadataEntry e) => new()
    {
        LinkId = e.LinkId,
        SourceType = e.SourceType,
        SourceDocumentId = e.SourceDocumentId,
        SourceAddress = e.SourceAddress,
        TargetType = e.TargetType,
        TargetDocumentId = e.TargetDocumentId,
        TargetAddress = e.TargetAddress,
        RefreshPolicy = e.RefreshPolicy,
        CreatedAtUtc = e.CreatedAtUtc,
        LastRefreshedAtUtc = e.LastRefreshedAtUtc
    };
}