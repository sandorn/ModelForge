using Microsoft.Extensions.Logging;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// PowerPoint COM 互操作服务。Phase C 实现跨应用链接和 Deck Check。
/// </summary>
public sealed class PowerPointInteropService : IDisposable
{
    private readonly OfficeApplicationFactory _factory;
    private readonly ILogger<PowerPointInteropService> _logger;
    private dynamic? _pptApp;

    public PowerPointInteropService(OfficeApplicationFactory factory, ILogger<PowerPointInteropService> logger)
    {
        _factory = factory;
        _logger = logger;
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
            _logger.LogWarning(ex, "获取活动演示文稿失败");
            return null;
        }
    }

    public void Dispose() { }
}
