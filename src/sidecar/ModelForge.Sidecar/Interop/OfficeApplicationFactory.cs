using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// Office 应用程序工厂。通过 oleaut32!GetActiveObject 连接运行中的 Office 实例。
/// 使用 dynamic 延迟绑定 + 显式 COM 释放，无需安装 Office PIA。
/// </summary>
public sealed class OfficeApplicationFactory : IDisposable
{
    private readonly List<object> _comObjects = new();
    private readonly ILogger<OfficeApplicationFactory> _logger;

    public OfficeApplicationFactory(ILogger<OfficeApplicationFactory> logger)
    {
        _logger = logger;
    }

    public dynamic? GetExcel() => GetRunningComObject(ComRuntime.CLSID.Excel, "Excel");
    public dynamic? GetPowerPoint() => GetRunningComObject(ComRuntime.CLSID.PowerPoint, "PowerPoint");
    public dynamic? GetWord() => GetRunningComObject(ComRuntime.CLSID.Word, "Word");

    private dynamic? GetRunningComObject(Guid clsid, string appName)
    {
        try
        {
            var obj = ComRuntime.GetActiveObject(clsid);
            if (obj != null)
            {
                Track(obj);
                return obj;
            }
            _logger.LogWarning("{AppName} 未运行", appName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法连接到 {AppName}", appName);
            return null;
        }
    }

    public T? Track<T>(T? comObject) where T : class
    {
        if (comObject != null && Marshal.IsComObject(comObject))
            _comObjects.Add(comObject);
        return comObject;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void Dispose()
    {
        for (int i = _comObjects.Count - 1; i >= 0; i--)
        {
            try
            {
                var obj = _comObjects[i];
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.FinalReleaseComObject(obj);
            }
            catch { }
        }
        _comObjects.Clear();
    }
}
