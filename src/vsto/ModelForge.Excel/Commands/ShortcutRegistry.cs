using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelForge.Excel.Commands
{
    public sealed class ShortcutRegistry
    {
        private readonly Dictionary<string, ShortcutDefinition> _shortcutsByChord = new Dictionary<string, ShortcutDefinition>(StringComparer.OrdinalIgnoreCase);

        public void RegisterDefaults()
        {
            foreach (var shortcut in DefaultShortcutMap.Create())
            {
                Register(shortcut);
            }
        }

        public void Register(ShortcutDefinition shortcut)
        {
            if (_shortcutsByChord.ContainsKey(shortcut.Shortcut))
            {
                throw new InvalidOperationException($"快捷键冲突：{shortcut.Shortcut}");
            }

            _shortcutsByChord[shortcut.Shortcut] = shortcut;
        }

        public IReadOnlyList<ShortcutDefinition> GetAll()
        {
            return _shortcutsByChord.Values.ToArray();
        }

        public Task<bool> TryHandleAsync(string shortcut)
        {
            // 阶段一仅建立注册表结构；真实键盘钩子需在 VSTO 工程中结合 Office 焦点状态实现。
            return Task.FromResult(_shortcutsByChord.ContainsKey(shortcut));
        }
    }
}
