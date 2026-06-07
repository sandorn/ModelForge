namespace ModelForge.Sidecar.Commands;

/// <summary>
/// 默认快捷键映射工厂。从原 VSTO 项目移植，快捷键注册表沿用相同数据格式。
/// </summary>
public static class DefaultShortcutMap
{
    public static IReadOnlyList<ShortcutDefinition> Create()
    {
        return new[]
        {
            new ShortcutDefinition(ExcelCommandIds.FillRight, "快速向右填充", "Ctrl+Alt+R"),
            new ShortcutDefinition(ExcelCommandIds.FillDown, "快速向下填充", "Ctrl+Alt+D"),
            new ShortcutDefinition(ExcelCommandIds.WrapIfError, "IFERROR 封装", "Ctrl+Alt+E"),
            new ShortcutDefinition(ExcelCommandIds.InsertStatistics, "插入统计摘要", "Ctrl+Alt+S"),
            new ShortcutDefinition(ExcelCommandIds.VisualizeInputs, "标记硬编码输入", "Ctrl+Alt+I"),
            new ShortcutDefinition(ExcelCommandIds.VisualizeFormulas, "标记公式", "Ctrl+Alt+F"),
            new ShortcutDefinition(ExcelCommandIds.NamesManager, "命名管理器", "Ctrl+Alt+W"),
            new ShortcutDefinition(ExcelCommandIds.VisualizeLinks, "标记外部链接", "Ctrl+Alt+L"),
            new ShortcutDefinition(ExcelCommandIds.ClearVisualizations, "清除审计标色", "Ctrl+Alt+C"),
            new ShortcutDefinition(ExcelCommandIds.ModelCheck, "运行 Model Check", "Ctrl+Alt+M"),
            new ShortcutDefinition(ExcelCommandIds.TracePrecedents, "追踪前驱", "Ctrl+Alt+P"),
            new ShortcutDefinition(ExcelCommandIds.TraceDependents, "追踪依赖", "Ctrl+Alt+T"),
            new ShortcutDefinition(ExcelCommandIds.ClearTrace, "清除追踪", "Ctrl+Alt+X"),
            new ShortcutDefinition(ExcelCommandIds.OptimizeWorkbook, "优化工作簿", "Ctrl+Alt+O"),
            new ShortcutDefinition(ExcelCommandIds.PrepareToShare, "安全外发副本", "Ctrl+Alt+H"),
            new ShortcutDefinition(ExcelCommandIds.ApplyFinanceFormat, "应用财务格式", "Ctrl+Alt+N"),
            new ShortcutDefinition(ExcelCommandIds.ToggleSign, "切换正负号", "Ctrl+Alt+G"),
            new ShortcutDefinition(ExcelCommandIds.InsertDcfTemplate, "插入 DCF 模板", "Ctrl+Alt+1"),
            new ShortcutDefinition(ExcelCommandIds.LinkToPowerPoint, "链接到 PowerPoint", "Ctrl+Alt+K"),
            new ShortcutDefinition(ExcelCommandIds.RefreshLinks, "刷新 Office 链接", "Ctrl+Alt+U"),
            new ShortcutDefinition(ExcelCommandIds.OpenTaskPane, "打开任务窗格", "Ctrl+Alt+B"),
            new ShortcutDefinition(PptCommandIds.GenerateAgenda, "生成 PowerPoint 目录", "Ctrl+Alt+Shift+A"),
            new ShortcutDefinition(PptCommandIds.DeckCheck, "PowerPoint Deck Check", "Ctrl+Alt+Shift+M"),
            new ShortcutDefinition(PptCommandIds.AlignLeft, "左对齐形状", "Ctrl+Alt+Shift+L"),
            new ShortcutDefinition(PptCommandIds.AlignCenter, "水平居中形状", "Ctrl+Alt+Shift+C"),
            new ShortcutDefinition(PptCommandIds.AlignRight, "右对齐形状", "Ctrl+Alt+Shift+R"),
            new ShortcutDefinition(PptCommandIds.AlignTop, "顶部对齐形状", "Ctrl+Alt+Shift+T"),
            new ShortcutDefinition(PptCommandIds.AlignMiddle, "垂直居中形状", "Ctrl+Alt+Shift+E"),
            new ShortcutDefinition(PptCommandIds.AlignBottom, "底部对齐形状", "Ctrl+Alt+Shift+B"),
            new ShortcutDefinition(PptCommandIds.DistributeHorizontal, "水平分布形状", "Ctrl+Alt+Shift+H"),
            new ShortcutDefinition(PptCommandIds.DistributeVertical, "垂直分布形状", "Ctrl+Alt+Shift+V"),
            new ShortcutDefinition(PptCommandIds.UnifyWidth, "统一形状宽度", "Ctrl+Alt+Shift+W"),
            new ShortcutDefinition(PptCommandIds.UnifyHeight, "统一形状高度", "Ctrl+Alt+Shift+Y"),
            new ShortcutDefinition(PptCommandIds.UnifySize, "统一形状尺寸", "Ctrl+Alt+Shift+Z"),
            new ShortcutDefinition(WordCommandIds.BuildDueDiligence, "生成尽调清单", "Ctrl+Alt+Shift+D"),
            new ShortcutDefinition(WordCommandIds.BuildCim, "生成 CIM 备忘录", "Ctrl+Alt+Shift+I"),
            new ShortcutDefinition(WordCommandIds.BuildManagementPresentation, "生成管理层演示大纲", "Ctrl+Alt+Shift+G"),
            new ShortcutDefinition(WordCommandIds.EmbedExcelRange, "Word 嵌入 Excel 区域", "Ctrl+Alt+Shift+K"),
            new ShortcutDefinition(WordCommandIds.RefreshLinks, "刷新 Word 链接", "Ctrl+Alt+Shift+U")
        };
    }
}
