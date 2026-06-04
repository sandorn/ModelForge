using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ModelForge.Excel.Infrastructure
{
    /// <summary>
    /// COM 对象释放作用域。复杂 Office COM 调用应把中间对象注册到此作用域，避免 Excel 进程残留。
    /// </summary>
    public sealed class ComObjectScope : IDisposable
    {
        private readonly Stack<object> _objects = new Stack<object>();

        public T Track<T>(T comObject) where T : class
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                _objects.Push(comObject);
            }

            return comObject;
        }

        public void Dispose()
        {
            while (_objects.Count > 0)
            {
                var instance = _objects.Pop();
                try
                {
                    Marshal.FinalReleaseComObject(instance);
                }
                catch (InvalidComObjectException)
                {
                    // 对象可能已经由宿主释放，忽略即可。
                }
            }
        }
    }
}
