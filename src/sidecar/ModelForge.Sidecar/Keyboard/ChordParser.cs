using System.Text;

namespace ModelForge.Sidecar.Keyboard;

/// <summary>
/// 键盘和弦解析器。将 Win32 虚拟键码 + 修饰键状态标准化为
/// "Ctrl+Alt+Shift+Key" 格式的字符串，供 ShortcutRegistry 精确匹配。
/// </summary>
public sealed class ChordParser
{
    /// <summary>Win32 虚拟键码常量（Keyboard 模块内部使用）。</summary>
    public static class VKey
    {
        public const uint Backspace = 0x08;
        public const uint Tab = 0x09;
        public const uint Enter = 0x0D;
        public const uint Shift = 0x10;
        public const uint Ctrl = 0x11;
        public const uint Alt = 0x12;
        public const uint Escape = 0x1B;
        public const uint Space = 0x20;
        public const uint PageUp = 0x21;
        public const uint PageDown = 0x22;
        public const uint End = 0x23;
        public const uint Home = 0x24;
        public const uint Left = 0x25;
        public const uint Up = 0x26;
        public const uint Right = 0x27;
        public const uint Down = 0x28;
        public const uint Insert = 0x2D;
        public const uint Delete = 0x2E;
        public const uint D0 = 0x30;
        public const uint D9 = 0x39;
        public const uint A = 0x41;
        public const uint Z = 0x5A;
        public const uint NumPad0 = 0x60;
        public const uint NumPad9 = 0x69;
        public const uint F1 = 0x70;
        public const uint F24 = 0x87;
        public const uint OemMinus = 0xBD;
        public const uint OemPlus = 0xBB;
        public const uint OemOpenBrackets = 0xDB;
        public const uint OemCloseBrackets = 0xDD;
        public const uint OemSemicolon = 0xBA;
        public const uint OemQuotes = 0xDE;
        public const uint OemComma = 0xBC;
        public const uint OemPeriod = 0xBE;
        public const uint OemQuestion = 0xBF;
        public const uint OemTilde = 0xC0;
    }

    /// <summary>
    /// 从原始键盘输入构造标准化和弦字符串。
    /// </summary>
    /// <param name="vkCode">Win32 虚拟键码</param>
    /// <param name="ctrl">Ctrl 是否按下</param>
    /// <param name="alt">Alt 是否按下</param>
    /// <param name="shift">Shift 是否按下</param>
    /// <returns>标准化和弦字符串，如 "Ctrl+Alt+R"；若无可打印键返回空字符串。</returns>
    public static string BuildChord(uint vkCode, bool ctrl, bool alt, bool shift)
    {
        var keyName = MapKeyName(vkCode);
        if (string.IsNullOrEmpty(keyName)) return string.Empty;

        var sb = new StringBuilder(20);

        if (ctrl) sb.Append("Ctrl+");
        if (alt) sb.Append("Alt+");
        if (shift) sb.Append("Shift+");
        sb.Append(keyName);

        return sb.ToString();
    }

    /// <summary>将 Win32 虚拟键码映射到显示名称。</summary>
    private static string? MapKeyName(uint vkCode)
    {
        // 数字键 0-9
        if (vkCode >= VKey.D0 && vkCode <= VKey.D9)
            return ((char)('0' + (vkCode - VKey.D0))).ToString();

        // 字母键 A-Z
        if (vkCode >= VKey.A && vkCode <= VKey.Z)
            return ((char)('A' + (vkCode - VKey.A))).ToString();

        // 功能键 F1-F24
        if (vkCode >= VKey.F1 && vkCode <= VKey.F24)
            return $"F{(int)(vkCode - VKey.F1) + 1}";

        // 小键盘数字 0-9
        if (vkCode >= VKey.NumPad0 && vkCode <= VKey.NumPad9)
            return $"NumPad{(int)(vkCode - VKey.NumPad0)}";

        return vkCode switch
        {
            VKey.OemMinus => "-",
            VKey.OemPlus => "=",
            VKey.OemOpenBrackets => "[",
            VKey.OemCloseBrackets => "]",
            VKey.OemSemicolon => ";",
            VKey.OemQuotes => "'",
            VKey.OemComma => ",",
            VKey.OemPeriod => ".",
            VKey.OemQuestion => "/",
            VKey.OemTilde => "`",
            VKey.Backspace => "Backspace",
            VKey.Enter => "Enter",
            VKey.Space => "Space",
            VKey.Tab => "Tab",
            VKey.Escape => "Esc",
            VKey.Delete => "Delete",
            VKey.Insert => "Insert",
            VKey.Home => "Home",
            VKey.End => "End",
            VKey.PageUp => "PageUp",
            VKey.PageDown => "PageDown",
            VKey.Left => "Left",
            VKey.Right => "Right",
            VKey.Up => "Up",
            VKey.Down => "Down",
            _ => null
        };
    }
}
