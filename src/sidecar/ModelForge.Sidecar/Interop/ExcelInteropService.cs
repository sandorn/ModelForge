using Microsoft.Extensions.Logging;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// Excel COM 互操作服务。通过 .NET 原生 dynamic + Marshal 执行 Excel 自动化。
/// 提供工作簿/工作表/选区查询及 Excel 版本信息获取。
/// </summary>
public sealed class ExcelInteropService : IDisposable
{
    private readonly OfficeApplicationFactory _factory;
    private readonly ILogger<ExcelInteropService> _logger;
    private dynamic? _excelApp;

    public ExcelInteropService(OfficeApplicationFactory factory, ILogger<ExcelInteropService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>获取当前 Excel Application 实例。</summary>
    public dynamic? GetApplication() => _excelApp ??= _factory.GetExcel();

    /// <summary>获取活动工作簿名称。</summary>
    public string? GetActiveWorkbookName()
    {
        try
        {
            var app = GetApplication();
            return app?.ActiveWorkbook?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取活动工作簿失败");
            return null;
        }
    }

    /// <summary>获取活动工作表名称。</summary>
    public string? GetActiveWorksheetName()
    {
        try
        {
            var app = GetApplication();
            return app?.ActiveSheet?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取活动工作表失败");
            return null;
        }
    }

    /// <summary>获取当前选中区域地址。</summary>
    public string? GetActiveSelectionAddress()
    {
        try
        {
            var app = GetApplication();
            dynamic? selection = app?.Selection;
            if (selection != null)
            {
                // Range 对象有 Address 属性
                return selection.Address;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取选中区域失败");
            return null;
        }
    }

    /// <summary>获取 Office 版本信息。</summary>
    public (string Name, string Version)? GetVersionInfo()
    {
        try
        {
            var app = GetApplication();
            if (app == null) return null;
            return ((string)app.Name, (string)app.Version);
        }
        catch
        {
            return null;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void Dispose()
    {
        _factory.Dispose();
    }
}
