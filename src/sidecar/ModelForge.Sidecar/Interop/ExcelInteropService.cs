using System.Diagnostics;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// Excel COM 互操作服务。通过 .NET 9 原生 dynamic + Marshal 执行 Excel 自动化。
/// Phase B 将在此基类上实现 Power Tools、Visualizations、Model Check 等具体操作。
/// </summary>
public sealed class ExcelInteropService : IDisposable
{
    private readonly OfficeApplicationFactory _factory;
    private dynamic? _excelApp;

    public ExcelInteropService(OfficeApplicationFactory factory)
    {
        _factory = factory;
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
            Trace.TraceWarning($"获取活动工作簿失败: {ex.Message}");
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
            Trace.TraceWarning($"获取活动工作表失败: {ex.Message}");
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
            Trace.TraceWarning($"获取选中区域失败: {ex.Message}");
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

    public void Dispose()
    {
        _factory.Dispose();
    }
}
