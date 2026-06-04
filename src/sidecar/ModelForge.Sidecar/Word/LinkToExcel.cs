using ModelForge.Sidecar.Interop;

namespace ModelForge.Sidecar.Word;

/// <summary>
/// Word Link to Excel — 将 Excel 表格作为 OLE 链接嵌入 Word。
/// </summary>
public static class LinkToExcel
{
    /// <summary>
    /// 将 Excel 当前选中区域链接嵌入到 Word 文档中。
    /// </summary>
    public static string EmbedExcelRange(dynamic excelApp, dynamic? wordApp = null)
    {
        if (wordApp == null)
        {
            wordApp = ComRuntime.GetActiveObject(ComRuntime.CLSID.Word);
            if (wordApp == null) return "Word 未运行。请先启动 Word。";
        }

        dynamic selection = excelApp.Selection;
        string rangeAddr = selection.Address;
        dynamic workbook = excelApp.ActiveWorkbook;

        // 复制 Excel Range
        selection.Copy();

        dynamic document = wordApp.ActiveDocument;
        if (document == null) return "Word 中没有打开的文档。";

        // 在 Word 中粘贴为 OLE 链接对象
        dynamic wordSelection = wordApp.Selection;
        wordSelection.PasteSpecial(
            Link: true,
            DataType: 0,     // wdPasteOLEObject
            Placement: 0,    // wdInLine
            DisplayAsIcon: false);

        return $"Excel Range '{workbook.Name}!{rangeAddr}' 已链接嵌入 Word 文档。";
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
