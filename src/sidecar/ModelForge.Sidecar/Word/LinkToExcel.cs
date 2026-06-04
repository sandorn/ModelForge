using ModelForge.Sidecar.Interop;
using ModelForge.Sidecar.Linking;

namespace ModelForge.Sidecar.Word;

/// <summary>
/// Word Link to Excel — 将 Excel 表格作为 OLE 链接嵌入 Word，并提供链接刷新。
/// </summary>
public static class LinkToExcel
{
    /// <summary>
    /// 将 Excel 当前选中区域链接嵌入到 Word 文档中。
    /// 委托给 <see cref="ExcelToWordLinker.LinkRange"/> 共享实现。
    /// </summary>
    public static string EmbedExcelRange(dynamic excelApp, dynamic? wordApp = null)
    {
        return ExcelToWordLinker.LinkRange(excelApp, wordApp);
    }

    /// <summary>
    /// 刷新 Word 文档中所有 OLE 链接。
    /// </summary>
    public static string RefreshLinks(dynamic? wordApp = null)
    {
        if (wordApp == null)
        {
            wordApp = ComRuntime.GetActiveObject(ComRuntime.CLSID.Word);
            if (wordApp == null) return "Word 未运行。";
        }

        dynamic document = wordApp.ActiveDocument;
        if (document == null) return "Word 中没有打开的文档。";

        int refreshed = 0;
        int broken = 0;
        foreach (dynamic field in document.Fields)
        {
            try
            {
                if (field.Type == 56) // wdFieldLink
                {
                    field.Update();
                    refreshed++;
                }
            }
            catch
            {
                broken++;
            }
        }

        return $"Word 链接刷新完成: {refreshed} 成功, {broken} 失败。";
    }
}
