using Microsoft.Extensions.Logging;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// Word COM 互操作服务。Phase C 实现 Link to Excel 和 Doc Builder。
/// </summary>
public sealed class WordInteropService : IDisposable
{
    private readonly OfficeApplicationFactory _factory;
    private readonly ILogger<WordInteropService> _logger;
    private dynamic? _wordApp;

    public WordInteropService(OfficeApplicationFactory factory, ILogger<WordInteropService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public dynamic? GetApplication()
    {
        _wordApp = _factory.GetWord();
        return _wordApp;
    }

    public string? GetActiveDocumentName()
    {
        try
        {
            return GetApplication()?.ActiveDocument?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取活动文档失败");
            return null;
        }
    }

    public void Dispose() { }
}
