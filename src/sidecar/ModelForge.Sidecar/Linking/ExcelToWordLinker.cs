namespace ModelForge.Sidecar.Linking;

/// <summary>
/// Excel → Word 链接器。
/// 将 Excel Range 作为 OLE 链接嵌入 Word 文档。
/// </summary>
public static class ExcelToWordLinker
{
    /// <summary>
    /// 将 Excel Range 链接到 Word 文档的指定位置。
    /// </summary>
    /// <param name="excelApp">Excel Application</param>
    /// <param name="wordApp">Word Application。若为 null 则自动连接。</param>
    /// <returns>操作结果描述。</returns>
    public static string LinkRange(dynamic excelApp, dynamic? wordApp = null)
    {
        if (wordApp == null)
        {
            try { wordApp = Interop.ComRuntime.GetActiveObject(Interop.ComRuntime.CLSID.Word); }
            catch (Exception ex)
            {
                return $"无法连接到 Word：{ex.Message}。请确认 Word 已启动。";
            }
        }

        if (wordApp == null) return "Word 未运行。";

        dynamic selection = excelApp.Selection;
        string rangeAddr = selection.Address;
        dynamic workbook = excelApp.ActiveWorkbook;
        string sheetName = excelApp.ActiveSheet.Name;

        // 复制
        selection.Copy();

        dynamic document = wordApp.ActiveDocument;
        if (document == null) return "Word 中没有打开的文档。";

        dynamic wordSelection = wordApp.Selection;

        // 粘贴为 OLE 链接（带源格式）
        wordSelection.PasteSpecial(
            Link: true,
            DataType: 0,    // wdPasteOLEObject = 0
            Placement: 0,   // wdInLine = 0
            DisplayAsIcon: false);

        return $"Range '{workbook.Name}!{sheetName}!{rangeAddr}' 已链接到 '{document.Name}'。";
    }
}
