namespace ModelForge.Sidecar.Commands;

/// <summary>
/// 快捷键定义数据类。从原 VSTO 项目不变移植。
/// </summary>
public sealed class ShortcutDefinition
{
    public ShortcutDefinition(string commandId, string displayName, string shortcut)
    {
        CommandId = commandId;
        DisplayName = displayName;
        Shortcut = shortcut;
    }

    /// <summary>对应的命令 ID，如 "excel.fill-right"。</summary>
    public string CommandId { get; }

    /// <summary>用户可见的显示名称，如 "快速向右填充"。</summary>
    public string DisplayName { get; }

    /// <summary>标准化快捷键组合字符串，如 "Ctrl+Alt+R"。</summary>
    public string Shortcut { get; }
}
