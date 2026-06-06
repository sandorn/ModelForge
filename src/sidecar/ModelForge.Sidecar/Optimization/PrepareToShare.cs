namespace ModelForge.Sidecar.Optimization;

/// <summary>
/// 安全外发副本生成器。创建公式转数值、清除批注和元数据的干净副本。
/// 原始工作簿永不修改。
/// </summary>
public static class PrepareToShare
{
    public sealed class PrepareResult
    {
        public string OutputPath { get; set; } = string.Empty;
        public int FormulasConverted { get; set; }
        public int CommentsRemoved { get; set; }
        public int HiddenRowsRemoved { get; set; }
        public int HiddenColumnsRemoved { get; set; }
        public int ExternalLinksBroken { get; set; }
        public int VeryHiddenSheetsRemoved { get; set; }
        public List<string> Actions { get; } = new();
    }

    /// <summary>
    /// 基于当前工作簿生成安全外发副本。
    /// </summary>
    /// <param name="excelApp">Excel Application</param>
    /// <param name="outputPath">副本保存路径。若为空，默认在源文件同目录生成 "_SafeCopy.xlsx"。</param>
    public static PrepareResult Execute(dynamic excelApp, string? outputPath = null)
    {
        var result = new PrepareResult();
        dynamic workbook = excelApp.ActiveWorkbook;

        // 计算默认输出路径
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            string originalPath = workbook.FullName;
            string dir = Path.GetDirectoryName(originalPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string name = Path.GetFileNameWithoutExtension(originalPath) ?? "Workbook";
            outputPath = Path.Combine(dir, $"{name}_SafeCopy.xlsx");
        }

        // 1. 另存副本 (关键：先保存，所有操作在副本上进行)
        workbook.SaveCopyAs(outputPath);
        result.OutputPath = outputPath;
        result.Actions.Add($"副本已保存: {outputPath}");

        // 2. 打开副本进行处理
        dynamic safeWb = excelApp.Workbooks.Open(outputPath);

        try
        {
            foreach (dynamic worksheet in safeWb.Worksheets)
            {
                try
                {
                    dynamic usedRange = worksheet.UsedRange;

                    // 公式 → 值
                    foreach (dynamic cell in usedRange)
                    {
                        if (cell.HasFormula)
                        {
                            cell.Value = cell.Value; // 公式转值
                            result.FormulasConverted++;
                        }
                    }

                    // 清除批注
                    try
                    {
                        int commentCount = worksheet.Comments.Count;
                        if (commentCount > 0)
                        {
                            foreach (dynamic comment in worksheet.Comments)
                            {
                                comment.Delete();
                            }
                            result.CommentsRemoved += commentCount;
                        }
                    }
                    catch { }
                }
                catch { /* 受保护的工作表 */ }
            }

            // 2.5 清除隐藏行和隐藏列
            foreach (dynamic worksheet in safeWb.Worksheets)
            {
                try
                {
                    dynamic usedRange = worksheet.UsedRange;
                    // 隐藏行
                    for (int r = usedRange.Rows.Count; r >= 1; r--)
                    {
                        try
                        {
                            if (worksheet.Rows[r].Hidden)
                            {
                                worksheet.Rows[r].Hidden = false;
                                worksheet.Rows[r].Delete();
                                result.HiddenRowsRemoved++;
                            }
                        }
                        catch { }
                    }
                    // 隐藏列
                    for (int c = usedRange.Columns.Count; c >= 1; c--)
                    {
                        try
                        {
                            if (worksheet.Columns[c].Hidden)
                            {
                                worksheet.Columns[c].Hidden = false;
                                worksheet.Columns[c].Delete();
                                result.HiddenColumnsRemoved++;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            // 3. 清除文档属性
            try { safeWb.BuiltinDocumentProperties("Author").Value = ""; } catch { }
            try { safeWb.BuiltinDocumentProperties("Company").Value = ""; } catch { }
            try { safeWb.BuiltinDocumentProperties("Last Author").Value = ""; } catch { }

            // 4. 删除隐藏和深度隐藏工作表
            var sheetsToDelete = new List<string>();
            foreach (dynamic sheet in safeWb.Sheets)
            {
                try
                {
                    int visibility = sheet.Visible;
                    if (visibility == 0 || visibility == 2) // xlSheetHidden or xlSheetVeryHidden
                    {
                        if (visibility == 2) result.VeryHiddenSheetsRemoved++;
                        sheetsToDelete.Add(sheet.Name);
                    }
                }
                catch { }
            }
            foreach (var sheetName in sheetsToDelete)
            {
                try
                {
                    safeWb.Sheets[sheetName].Visible = -1; // xlSheetVisible
                    safeWb.Sheets[sheetName].Delete();
                }
                catch { }
            }

            // 5. 断裂外部链接
            try
            {
                dynamic linkSources = safeWb.LinkSources(1); // xlExcelLinks
                if (linkSources != null)
                {
                    foreach (dynamic link in linkSources)
                    {
                        try
                        {
                            safeWb.BreakLink(link, 1);
                            result.ExternalLinksBroken++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 6. 保存并关闭副本
            safeWb.Save();
            safeWb.Close();

            result.Actions.Add($"已转换 {result.FormulasConverted} 个公式为值");
            result.Actions.Add($"已清除 {result.CommentsRemoved} 个批注");
            result.Actions.Add($"已删除 {result.HiddenRowsRemoved} 个隐藏行");
            result.Actions.Add($"已删除 {result.HiddenColumnsRemoved} 个隐藏列");
            result.Actions.Add($"已断裂 {result.ExternalLinksBroken} 个外部链接");
            result.Actions.Add($"已删除 {sheetsToDelete.Count} 个隐藏工作表（含 {result.VeryHiddenSheetsRemoved} 个深度隐藏）");
        }
        finally
        {
            // 确保副本已关闭
            try { safeWb.Close(false); } catch { }
        }

        return result;
    }
}
