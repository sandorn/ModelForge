using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Office = Microsoft.Office.Core;

namespace ModelForge.Excel.Ribbon
{
    /// <summary>
    /// VSTO Ribbon XML 入口。Office 通过 IRibbonExtensibility 加载 XML 并反射调用同名回调。
    /// </summary>
    public sealed class ModelForgeRibbon : Office.IRibbonExtensibility
    {
        private const string RibbonResourceName = "ModelForge.Excel.Ribbon.ModelForgeRibbon.xml";
        private readonly ModelForgeRibbonCallbacks _callbacks;

        public ModelForgeRibbon(ModelForgeRibbonCallbacks callbacks)
        {
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
        }

        public string GetCustomUI(string ribbonId)
        {
            return LoadRibbonXml();
        }

        public void OnRibbonLoad(Office.IRibbonUI ribbon)
        {
            _callbacks.OnRibbonLoad(ribbon);
        }

        public void OnPingBackend(Office.IRibbonControl control)
        {
            _callbacks.OnPingBackend(control);
        }

        public void OnOpenTaskPane(Office.IRibbonControl control)
        {
            _callbacks.OnOpenTaskPane(control);
        }

        public void OnDispatchCommand(Office.IRibbonControl control)
        {
            _callbacks.OnDispatchCommand(control);
        }

        private static string LoadRibbonXml()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => string.Equals(name, RibbonResourceName, StringComparison.Ordinal))
                ?? assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(".ModelForgeRibbon.xml", StringComparison.Ordinal));

            if (resourceName == null)
            {
                Trace.TraceError("ModelForge Ribbon XML 嵌入资源未找到。");
                return string.Empty;
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Trace.TraceError("ModelForge Ribbon XML 嵌入资源无法打开：{0}", resourceName);
                    return string.Empty;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}