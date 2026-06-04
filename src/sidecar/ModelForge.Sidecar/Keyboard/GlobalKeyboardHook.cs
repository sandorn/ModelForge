using System.Runtime.InteropServices;
using ModelForge.Sidecar.Commands;

namespace ModelForge.Sidecar.Keyboard;

/// <summary>
/// 全局低级键盘钩子 (WH_KEYBOARD_LL)。
/// 作为 IHostedService 运行，在独立线程上监听系统键盘事件。
/// </summary>
public sealed class GlobalKeyboardHook : IHostedService, IDisposable
{
    private readonly ShortcutRegistry _shortcutRegistry;
    private readonly KeyboardCommandRouter _commandRouter;
    private readonly ILogger<GlobalKeyboardHook> _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _hookTask;
    private IntPtr _hookId = IntPtr.Zero;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    public GlobalKeyboardHook(
        ShortcutRegistry shortcutRegistry,
        KeyboardCommandRouter commandRouter,
        ILogger<GlobalKeyboardHook> logger)
    {
        _shortcutRegistry = shortcutRegistry;
        _commandRouter = commandRouter;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _shortcutRegistry.RegisterDefaults();
        _logger.LogInformation(
            "全局键盘钩子已启动，注册 {Count} 个快捷键", _shortcutRegistry.GetAll().Count);

        _hookTask = Task.Run(() => RunMessageLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("全局键盘钩子已停止");
        _cts.Cancel();
        Unhook();
        return _hookTask ?? Task.CompletedTask;
    }

    private void RunMessageLoop(CancellationToken ct)
    {
        _logger.LogDebug("键盘钩子消息循环启动");

        _hookId = NativeMethods.SetWindowsHookEx(
            WH_KEYBOARD_LL,
            HookCallback,
            IntPtr.Zero,
            0);

        if (_hookId == IntPtr.Zero)
        {
            _logger.LogWarning("SetWindowsHookEx 失败，键盘钩子未安装");
            return;
        }

        // Windows 消息泵：WH_KEYBOARD_LL 需要消息循环
        while (!ct.IsCancellationRequested)
        {
            NativeMethods.MSG msg;
            if (NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, 1))
            {
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }
            else
            {
                Thread.Sleep(10);
            }
        }

        Unhook();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // 跳过重复按键（auto-repeat）
            bool isRepeat = (kb.flags & 0x80) != 0;
            if (!isRepeat)
            {
                bool ctrl = (NativeMethods.GetKeyState(ChordParser.VKey.Ctrl) & 0x8000) != 0;
                bool alt = (NativeMethods.GetKeyState(ChordParser.VKey.Alt) & 0x8000) != 0;
                bool shift = (NativeMethods.GetKeyState(ChordParser.VKey.Shift) & 0x8000) != 0;

                var chord = ChordParser.BuildChord(kb.vkCode, ctrl, alt, shift);
                if (!string.IsNullOrEmpty(chord))
                {
                    var shortcut = _shortcutRegistry.FindByChord(chord);
                    if (shortcut != null)
                    {
                        _logger.LogDebug("快捷键触发: {Chord} → {Command}", chord, shortcut.CommandId);
                        _ = _commandRouter.RouteAsync(shortcut.CommandId);
                        return (IntPtr)1; // 吞掉此按键
                    }
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void Unhook()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    public void Dispose() => _cts.Dispose();

    private static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(uint nVirtKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd,
            uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DispatchMessage(ref MSG lpMsg);

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }
    }
}
