using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ModelForge.Contracts;

namespace ModelForge.Sidecar.Services;

/// <summary>
/// 后端桥接 HTTP 客户端。通过 ASP.NET Core 后端 API 上报审计事件、同步配置和链接元数据。
/// </summary>
public sealed class BackendBridgeClient
{
    private readonly HttpClient _httpClient;

    public BackendBridgeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>健康检查。</summary>
    public async Task<string> GetHealthAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    /// <summary>分发命令到后端桥接层。</summary>
    public async Task<string> DispatchCommandAsync(string commandId, OfficeHost host, CancellationToken ct = default)
    {
        var payload = new CommandDispatchRequest
        {
            CommandId = commandId,
            Host = host,
            UserId = "local-sidecar"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/commands/dispatch");
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));
        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    /// <summary>上报审计事件。</summary>
    public async Task ReportAuditEventAsync(AuditEventRequest auditEvent, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/audit-events");
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));
        request.Content = JsonContent.Create(auditEvent);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>获取命令目录。</summary>
    public async Task<IReadOnlyList<CommandDefinition>> GetCommandsAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/commands");
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<CommandDefinition>>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct).ConfigureAwait(false);

        return envelope?.Data ?? (IReadOnlyList<CommandDefinition>)Array.Empty<CommandDefinition>();
    }

    /// <summary>获取后端记录的 Excel ↔ PPT/Word 链接元数据。</summary>
    public async Task<IReadOnlyList<LinkMetadata>> GetLinkMetadataAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/links");
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<LinkMetadata>>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct).ConfigureAwait(false);

        return envelope?.Data ?? (IReadOnlyList<LinkMetadata>)Array.Empty<LinkMetadata>();
    }

    /// <summary>读取后端企业词典术语；需要 Sidecar 配置 ServiceToken。</summary>
    public async Task<IReadOnlyList<DictionaryTerm>> GetDictionaryTermsAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dictionary/service-export");
        request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<DictionaryExportResponse>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct).ConfigureAwait(false);

        return envelope?.Data?.Terms.ToArray() ?? (IReadOnlyList<DictionaryTerm>)Array.Empty<DictionaryTerm>();
    }
}
