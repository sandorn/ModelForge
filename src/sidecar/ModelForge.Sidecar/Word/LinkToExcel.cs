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
        var result = wordApp == null
            ? RefreshLinkFields()
            : RefreshLinkFields((object?)wordApp);
        return $"Word 链接刷新完成: {result.Refreshed} 成功, {result.Broken} 失败。";
    }

    /// <summary>
    /// 按后端 LinkMetadata 指定的 Word 目标精准刷新；目标地址不足时回退全量字段扫描。
    /// </summary>
    public static LinkRefresher.RefreshResult RefreshLinkFields(
        IEnumerable<LinkRefreshPlanner.WordTarget>? targets = null)
    {
        var wordApp = ComRuntime.GetActiveObject(ComRuntime.CLSID.Word);
        return RefreshLinkFields((object?)wordApp, targets);
    }

    public static LinkRefresher.RefreshResult RefreshLinkFields(
        object? wordApp,
        IEnumerable<LinkRefreshPlanner.WordTarget>? targets = null)
    {
        var result = new LinkRefresher.RefreshResult();
        if (wordApp == null)
        {
            result.BrokenDetails.Add("Word 未运行。");
            return result;
        }

        try
        {
            dynamic word = wordApp;
            dynamic document = word.ActiveDocument;
            if (document == null)
            {
                result.BrokenDetails.Add("Word 中没有打开的文档。");
                return result;
            }

            var targetArray = targets?.ToArray() ?? Array.Empty<LinkRefreshPlanner.WordTarget>();
            if (targetArray.Length > 0)
            {
                if (targetArray.Any(target => !target.IsPrecise))
                {
                    var fallback = RefreshAllFields(document);
                    fallback.BrokenDetails.Insert(0, "部分 Word 链接元数据缺少可定位的 targetAddress，已回退全量刷新。");
                    return fallback;
                }

                foreach (var target in targetArray)
                {
                    result.TotalLinks++;
                    string failure;
                    var field = FindWordLinkField(document, target, out failure);
                    if (field == null)
                    {
                        result.Broken++;
                        result.BrokenDetails.Add($"Link {target.LinkId}: {failure}");
                        continue;
                    }

                    UpdateWordField(field, result, $"Link {target.LinkId}");
                }

                return result;
            }

            return RefreshAllFields(document);
        }
        catch (Exception ex)
        {
            result.BrokenDetails.Add($"Word 链接刷新失败: {ex.Message}");
        }

        return result;
    }

    private static LinkRefresher.RefreshResult RefreshAllFields(dynamic document)
    {
        var result = new LinkRefresher.RefreshResult();
        foreach (dynamic field in document.Fields)
        {
            try
            {
                if (field.Type == 56)
                {
                    result.TotalLinks++;
                    UpdateWordField(field, result, "Word field");
                }
            }
            catch
            {
            }
        }

        return result;
    }

    private static dynamic? FindWordLinkField(
        dynamic document,
        LinkRefreshPlanner.WordTarget target,
        out string failure)
    {
        failure = string.Empty;

        try
        {
            if (target.FieldIndex.HasValue)
            {
                return document.Fields[target.FieldIndex.Value];
            }

            if (target.InlineShapeIndex.HasValue)
            {
                dynamic inlineShape = document.InlineShapes[target.InlineShapeIndex.Value];
                return inlineShape.Field;
            }

            if (target.TableIndex.HasValue)
            {
                dynamic table = document.Tables[target.TableIndex.Value];
                foreach (dynamic field in table.Range.Fields)
                {
                    if (field.Type == 56)
                    {
                        return field;
                    }
                }

                failure = $"表格 {target.TableIndex.Value} 中未找到链接字段。";
                return null;
            }

            failure = $"无法解析 Word 目标对象 {target.TargetAddress}。";
            return null;
        }
        catch (Exception ex)
        {
            failure = $"Word 目标对象定位失败 {target.TargetAddress}: {ex.Message}";
            return null;
        }
    }

    private static void UpdateWordField(dynamic field, LinkRefresher.RefreshResult result, string label)
    {
        try
        {
            if (field.Type != 56)
            {
                result.Broken++;
                result.BrokenDetails.Add($"{label}: 目标字段不是链接字段。");
                return;
            }

            field.Update();
            result.Refreshed++;
        }
        catch (Exception ex)
        {
            result.Broken++;
            result.BrokenDetails.Add($"{label}: 链接刷新失败: {ex.Message}");
        }
    }
}
