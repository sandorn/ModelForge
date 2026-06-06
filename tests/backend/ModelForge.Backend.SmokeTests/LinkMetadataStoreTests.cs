using ModelForge.Backend.Services;
using ModelForge.Contracts;
using Xunit;

namespace ModelForge.Backend.Tests.Services;

public class LinkMetadataStoreTests
{
    [Fact]
    public async Task CreateAsync_ReturnsLinkMetadataWithId()
    {
        var store = new InMemoryLinkMetadataStore();
        var request = new CreateLinkMetadataRequest
        {
            SourceType = LinkSourceType.ExcelRange,
            SourceDocumentId = "workbook-1",
            SourceAddress = "Sheet1!A1:C10",
            TargetType = LinkTargetType.PowerPointShape,
            TargetDocumentId = "deck-1",
            TargetAddress = "Slide2/Shape3"
        };

        var result = await store.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.LinkId);
        Assert.Equal("workbook-1", result.SourceDocumentId);
        Assert.Equal("deck-1", result.TargetDocumentId);
        Assert.Equal("manual", result.RefreshPolicy);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCreatedLinks()
    {
        var store = new InMemoryLinkMetadataStore();
        var request = new CreateLinkMetadataRequest
        {
            SourceType = LinkSourceType.ExcelChart,
            SourceDocumentId = "workbook-1",
            SourceAddress = "Chart1",
            TargetType = LinkTargetType.PowerPointChart,
            TargetDocumentId = "deck-1",
            TargetAddress = "Slide3/Chart2"
        };
        await store.CreateAsync(request, CancellationToken.None);

        var links = await store.GetAllAsync(CancellationToken.None);

        Assert.Single(links);
        Assert.Equal("Chart1", links.First().SourceAddress);
    }

    [Fact]
    public async Task MarkRefreshRequestedAsync_ExistingLink_ReturnsAccepted()
    {
        var store = new InMemoryLinkMetadataStore();
        var createReq = new CreateLinkMetadataRequest
        {
            SourceType = LinkSourceType.ExcelRange,
            SourceDocumentId = "workbook-1",
            SourceAddress = "A1",
            TargetType = LinkTargetType.WordTable,
            TargetDocumentId = "doc-1",
            TargetAddress = "Table1"
        };
        var created = await store.CreateAsync(createReq, CancellationToken.None);

        var refreshReq = new LinkRefreshRequest
        {
            LinkId = created.LinkId,
            RequestedBy = "user-1"
        };
        var result = await store.MarkRefreshRequestedAsync(refreshReq, CancellationToken.None);

        Assert.Equal(CommandStatus.Accepted, result.Status);
        Assert.Equal(created.LinkId, result.LinkId);
    }

    [Fact]
    public async Task MarkRefreshRequestedAsync_NonExistentLink_ReturnsFailed()
    {
        var store = new InMemoryLinkMetadataStore();
        var refreshReq = new LinkRefreshRequest
        {
            LinkId = "nonexistent-id",
            RequestedBy = "user-1"
        };

        var result = await store.MarkRefreshRequestedAsync(refreshReq, CancellationToken.None);

        Assert.Equal(CommandStatus.Failed, result.Status);
    }

    [Fact]
    public async Task CreateAsync_SupportsCancellation()
    {
        var store = new InMemoryLinkMetadataStore();
        var request = new CreateLinkMetadataRequest
        {
            SourceType = LinkSourceType.ExcelRange,
            SourceDocumentId = "workbook-1",
            SourceAddress = "A1",
            TargetType = LinkTargetType.WordTable,
            TargetDocumentId = "doc-1",
            TargetAddress = "Table1"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.CreateAsync(request, cts.Token));
    }
}
