using System;
using System.Diagnostics;
using System.Security.Permissions;
using Microsoft.VisualStudio.Tools.Applications.Runtime;

namespace ModelForge.Excel
{
    /// <summary>
    /// VSTO 启动对象最小设计器代码。
    /// 完整 Visual Studio VSTO 模板可在后续迁移时重新生成该文件。
    /// </summary>
    [StartupObject(0)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public sealed partial class ThisAddIn : Microsoft.Office.Tools.AddInBase
    {
        public ThisAddIn(Microsoft.Office.Tools.Factory factory, IServiceProvider serviceProvider)
            : base(factory, serviceProvider, "AddIn", "ThisAddIn")
        {
            Globals.ThisAddIn = this;
            InternalStartup();
        }

        protected override void Initialize()
        {
            base.Initialize();
            System.Windows.Forms.Application.EnableVisualStyles();
        }

        protected override void OnShutdown()
        {
            try
            {
                base.OnShutdown();
            }
            finally
            {
                Globals.ThisAddIn = null;
            }
        }

        [DebuggerNonUserCode]
        private void InternalStartup()
        {
            Startup += ThisAddIn_Startup;
            Shutdown += ThisAddIn_Shutdown;
        }
    }

    /// <summary>
    /// 与 VSTO 模板保持一致的全局访问入口，便于 Ribbon 或后续 COM 自动化代码定位当前插件实例。
    /// </summary>
    internal static partial class Globals
    {
        internal static ThisAddIn ThisAddIn { get; set; }
    }
}