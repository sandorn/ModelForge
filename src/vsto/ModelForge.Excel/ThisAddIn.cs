using System;
using System.Diagnostics;
using Microsoft.Office.Core;
using ModelForge.Excel.Commands;
using ModelForge.Excel.Configuration;
using ModelForge.Excel.Infrastructure;
using ModelForge.Excel.Ribbon;
using ModelForge.Excel.Services;

namespace ModelForge.Excel
{
    /// <summary>
    /// Excel VSTO 插件入口。负责初始化本地后端桥接、快捷键注册表和 Ribbon 回调。
    /// </summary>
    public sealed partial class ThisAddIn
    {
        private BackendBridgeClient _bridgeClient;
        private ShortcutRegistry _shortcutRegistry;
        private OfficeVersionInfo _officeVersionInfo;
        private ModelForgeRibbonCallbacks _ribbonCallbacks;

        /// <summary>
        /// 获取 Excel Application COM 对象。当前阶段只用于版本检测，复杂 COM 调用必须配合 ComObjectScope。
        /// </summary>
        public object ExcelApplication
        {
            get
            {
                try
                {
                    return GetHostItem<object>(typeof(object), "Application");
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("ModelForge 无法获取 Excel Application：{0}", ex.Message);
                    return null;
                }
            }
        }

        protected override IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            EnsureServicesInitialized();
            return new ModelForgeRibbon(_ribbonCallbacks);
        }

        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            EnsureServicesInitialized();
            _officeVersionInfo = OfficeVersionInfo.FromApplication(ExcelApplication);

            Trace.TraceInformation(
                "ModelForge Excel VSTO 已启动：{0} {1}，64 位进程={2}",
                _officeVersionInfo.ApplicationName,
                _officeVersionInfo.Version,
                _officeVersionInfo.Is64BitProcess);
        }

        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            Trace.TraceInformation("ModelForge Excel VSTO 正在关闭。");

            if (_bridgeClient != null)
            {
                _bridgeClient.Dispose();
                _bridgeClient = null;
            }

            _ribbonCallbacks = null;
            _shortcutRegistry = null;
            _officeVersionInfo = null;
        }

        private void EnsureServicesInitialized()
        {
            if (_bridgeClient == null)
            {
                _bridgeClient = new BackendBridgeClient(new BridgeOptions());
            }

            if (_shortcutRegistry == null)
            {
                _shortcutRegistry = new ShortcutRegistry();
                _shortcutRegistry.RegisterDefaults();
            }

            if (_ribbonCallbacks == null)
            {
                _ribbonCallbacks = new ModelForgeRibbonCallbacks(_bridgeClient);
            }
        }
    }
}