using System.Collections.Concurrent;

namespace ModelForge.Sidecar.Commands;

/// <summary>
/// 快捷键注册表。从原 VSTO 项目移植，新增线程安全支持和批量操作。
/// 全局键盘钩子捕获和弦后通过此注册表查找对应命令。
/// </summary>
public sealed class ShortcutRegistry
{
    private readonly ConcurrentDictionary<string, ShortcutDefinition> _shortcutsByChord
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>注册所有默认快捷键。</summary>
    public void RegisterDefaults()
    {
        if (!_shortcutsByChord.IsEmpty)
            return;

        foreach (var shortcut in DefaultShortcutMap.Create())
        {
            Register(shortcut);
        }
    }

    /// <summary>注册单个快捷键。冲突时抛出异常。</summary>
    public void Register(ShortcutDefinition shortcut)
    {
        if (!_shortcutsByChord.TryAdd(shortcut.Shortcut, shortcut))
        {
            var existing = _shortcutsByChord[shortcut.Shortcut];
            throw new InvalidOperationException(
                $"快捷键冲突：'{shortcut.Shortcut}' 已被 '{existing.DisplayName}' ({existing.CommandId}) 占用，" +
                $"无法注册 '{shortcut.DisplayName}' ({shortcut.CommandId})。");
        }
    }

    /// <summary>按和弦查找快捷键定义，不区分大小写。</summary>
    public ShortcutDefinition? FindByChord(string chord)
    {
        _shortcutsByChord.TryGetValue(chord, out var definition);
        return definition;
    }

    /// <summary>获取所有已注册快捷键。</summary>
    public IReadOnlyList<ShortcutDefinition> GetAll()
    {
        return _shortcutsByChord.Values.ToArray();
    }

    /// <summary>替换全部快捷键（用于用户自定义导入）。</summary>
    public void ReplaceAll(IEnumerable<ShortcutDefinition> shortcuts)
    {
        var nextShortcuts = shortcuts.ToArray();
        var validated = new Dictionary<string, ShortcutDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var shortcut in nextShortcuts)
        {
            if (string.IsNullOrWhiteSpace(shortcut.CommandId) ||
                string.IsNullOrWhiteSpace(shortcut.DisplayName) ||
                string.IsNullOrWhiteSpace(shortcut.Shortcut))
            {
                throw new InvalidOperationException("commandId, displayName, and shortcut are required.");
            }

            if (!validated.TryAdd(shortcut.Shortcut, shortcut))
            {
                var existing = validated[shortcut.Shortcut];
                throw new InvalidOperationException(
                    $"快捷键冲突：'{shortcut.Shortcut}' 已被 '{existing.DisplayName}' ({existing.CommandId}) 占用，" +
                    $"无法注册 '{shortcut.DisplayName}' ({shortcut.CommandId})。");
            }
        }

        _shortcutsByChord.Clear();
        foreach (var shortcut in nextShortcuts)
        {
            Register(shortcut);
        }
    }
}
