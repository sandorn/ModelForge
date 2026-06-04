using ModelForge.Contracts;
using ModelForge.Sidecar.Services;

namespace ModelForge.Sidecar.Keyboard;

/// <summary>
/// 键盘命令路由器。将快捷键和弦匹配结果分发到后端桥接层。
/// Phase B 将增加 Sidecar 本地命令执行（Power Tools, Visualizations 等）。
/// </summary>
public sealed class KeyboardCommandRouter
{
    private readonly BackendBridgeClient _bridgeClient;
    private readonly ILogger<KeyboardCommandRouter> _logger;

    public KeyboardCommandRouter(
        BackendBridgeClient bridgeClient,
        ILogger<KeyboardCommandRouter> logger)
    {
        _bridgeClient = bridgeClient;
        _logger = logger;
    }

    /// <summary>
    /// 将命令 ID 转发到后端桥接 API。
    /// </summary>
    public async Task RouteAsync(string commandId)
    {
        try
        {
            var response = await _bridgeClient.DispatchCommandAsync(
                commandId, OfficeHost.Excel).ConfigureAwait(false);

            _logger.LogDebug("命令已分发: {CommandId}, 响应: {Response}",
                commandId, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "命令分发失败: {CommandId}", commandId);
        }
    }
}
