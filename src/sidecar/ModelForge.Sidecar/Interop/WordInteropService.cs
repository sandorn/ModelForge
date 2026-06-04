using System.Diagnostics;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// Word COM 互操作服务。Phase C 实现 Link to Excel 和 Doc Builder。
/// </summary>
public sealed class WordInteropService : IDisposable
{
    private readonly OfficeApplicationFactory _factory;
    private dynamic? _wordApp;

    public WordInteropService(OfficeApplicationFactory factory)
    {
        _factory = factory;
    }

    public dynamic? GetApplication() => _wordApp ??= _factory.GetWord();

    public string? GetActiveDocumentName()
    {
        try
        {
            return GetApplication()?.ActiveDocument?.Name;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"获取活动文档失败: {ex.Message}");
            return null;
        }
    }

    public void Dispose() { }
}
