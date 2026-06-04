using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModelForge.Excel.Commands;
using ModelForge.Excel.Services;
using Office = Microsoft.Office.Core;

namespace ModelForge.Excel.Ribbon
{
    /// <summary>
    /// ModelForge Ribbon 回调。由 ModelForgeRibbon 暴露给 Office Ribbon XML。
    /// </summary>
    public sealed class ModelForgeRibbonCallbacks
    {
        private readonly BackendBridgeClient _bridgeClient;
        private Office.IRibbonUI _ribbon;

        public ModelForgeRibbonCallbacks(BackendBridgeClient bridgeClient)
        {
            _bridgeClient = bridgeClient;
        }

        public void OnRibbonLoad(Office.IRibbonUI ribbon)
        {
            _ribbon = ribbon;
            Trace.TraceInformation("ModelForge Ribbon 已加载。");
        }

        public async void OnPingBackend(Office.IRibbonControl control)
        {
            await ExecuteSafelyAsync("检查后端", async () =>
            {
                var health = await _bridgeClient.GetHealthAsync().ConfigureAwait(false);
                Trace.TraceInformation("ModelForge 后端健康检查成功：{0}", health);
            }).ConfigureAwait(false);
        }

        public async void OnDispatchCommand(Office.IRibbonControl control)
        {
            var commandId = RibbonControlTagReader.ReadTag(control);
            if (string.IsNullOrWhiteSpace(commandId))
            {
                Trace.TraceWarning("ModelForge Ribbon 命令缺少 tag，controlId={0}", control?.Id);
                return;
            }

            await ExecuteSafelyAsync(commandId, async () =>
            {
                var response = await _bridgeClient.DispatchCommandAsync(commandId, OfficeCommandHost.Excel).ConfigureAwait(false);
                Trace.TraceInformation("ModelForge 命令已分发：{0}，响应={1}", commandId, response);
            }).ConfigureAwait(false);
        }

        public async void OnOpenTaskPane(Office.IRibbonControl control)
        {
            // Web Add-in 任务窗格由 Office.js manifest 承载；VSTO 侧不直接操作 Web Add-in 进程。
            // 阶段一先通过后端桥接分发命令，后续由 Web 侧轮询或 SignalR 响应。
            await ExecuteSafelyAsync(ExcelCommandIds.OpenTaskPane, async () =>
            {
                var response = await _bridgeClient.DispatchCommandAsync(ExcelCommandIds.OpenTaskPane, OfficeCommandHost.Excel).ConfigureAwait(false);
                Trace.TraceInformation("ModelForge 打开任务窗格命令已分发：{0}", response);
            }).ConfigureAwait(false);
        }

        private static async Task ExecuteSafelyAsync(string operationName, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceError("ModelForge Ribbon 操作失败：{0}。{1}", operationName, ex);
                MessageBox.Show(
                    "ModelForge 操作失败：" + operationName + Environment.NewLine + ex.Message,
                    "ModelForge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
