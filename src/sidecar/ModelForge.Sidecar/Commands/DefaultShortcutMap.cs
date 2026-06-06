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
            new ShortcutDefinition(ExcelCommandIds.OpenTaskPane, "打开任务窗格", "Ctrl+Alt+B")
        };
    }
}
