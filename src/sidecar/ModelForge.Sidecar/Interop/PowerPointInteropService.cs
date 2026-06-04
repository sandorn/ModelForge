using System.Diagnostics;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// PowerPoint COM 互操作服务。Phase C 实现跨应用链接和 Deck Check。
/// </summary>
public sealed class PowerPointInteropService : IDisposable
{
    private readonly OfficeApplicationFactory _factory;
    private dynamic? _pptApp;

    public PowerPointInteropService(OfficeApplicationFactory factory)
    {
        _factory = factory;
    }

    public dynamic? GetApplication() => _pptApp ??= _factory.GetPowerPoint();

    public string? GetActivePresentationName()
    {
        try
        {
            return GetApplication()?.ActivePresentation?.Name;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"获取活动演示文稿失败: {ex.Message}");
            return null;
        }
    }

    public void Dispose() { }
}
