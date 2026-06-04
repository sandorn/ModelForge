using System.Runtime.InteropServices;

namespace ModelForge.Sidecar.Interop;

/// <summary>
/// COM 运行时辅助方法。提供 GetActiveObject P/Invoke 封装。
/// </summary>
public static class ComRuntime
{
    /// <summary>COM CLSID 常量。</summary>
    public static class CLSID
    {
        public static readonly Guid Excel = new("00024500-0000-0000-C000-000000000046");
        public static readonly Guid PowerPoint = new("91493441-5A91-11CF-8700-00AA0060263B");
        public static readonly Guid Word = new("000209FF-0000-0000-C000-000000000046");
    }

    /// <summary>
    /// 获取运行中的 COM 对象（oleaut32!GetActiveObject）。
    /// </summary>
    /// <returns>成功返回 COM 对象；失败返回 null。</returns>
    public static dynamic? GetActiveObject(Guid clsid)
    {
        var local = clsid;
        int hr = Native.GetActiveObject(ref local, IntPtr.Zero, out var obj);
        if (hr >= 0 && obj != null)
            return (dynamic)obj;
        return null;
    }

    private static class Native
    {
        [DllImport("oleaut32.dll", PreserveSig = true)]
        public static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    }
}
