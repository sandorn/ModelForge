using System.Collections.Concurrent;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public interface ILinkMetadataStore
{
    Task<IReadOnlyCollection<LinkMetadata>> GetAllAsync(CancellationToken cancellationToken);

    Task<LinkMetadata> CreateAsync(CreateLinkMetadataRequest request, CancellationToken cancellationToken);

    Task<LinkRefreshResponse> MarkRefreshRequestedAsync(LinkRefreshRequest request, CancellationToken cancellationToken);
}

public sealed class InMemoryLinkMetadataStore : ILinkMetadataStore
{
    private readonly ConcurrentDictionary<string, LinkMetadata> _links = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyCollection<LinkMetadata>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<LinkMetadata>>(_links.Values.ToArray());
    }

    public Task<LinkMetadata> CreateAsync(CreateLinkMetadataRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metadata = new LinkMetadata
        {
            LinkId = Guid.NewGuid().ToString("N"),
            SourceType = request.SourceType,
            SourceDocumentId = request.SourceDocumentId,
            SourceAddress = request.SourceAddress,
            TargetType = request.TargetType,
            TargetDocumentId = request.TargetDocumentId,
            TargetAddress = request.TargetAddress,
            RefreshPolicy = string.IsNullOrWhiteSpace(request.RefreshPolicy) ? "manual" : request.RefreshPolicy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _links[metadata.LinkId] = metadata;
        return Task.FromResult(metadata);
    }

    public Task<LinkRefreshResponse> MarkRefreshRequestedAsync(LinkRefreshRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_links.TryGetValue(request.LinkId, out var metadata))
        {
            return Task.FromResult(new LinkRefreshResponse
            {
                LinkId = request.LinkId,
                Status = CommandStatus.Failed,
                Message = "链接元数据不存在，无法刷新。"
            });
        }

        metadata.LastRefreshedAtUtc = DateTimeOffset.UtcNow;
        return Task.FromResult(new LinkRefreshResponse
        {
            LinkId = request.LinkId,
            Status = CommandStatus.Accepted,
            Message = "刷新请求已记录。实际 Office 对象刷新将在 VSTO 执行端完成。"
        });
    }
}
