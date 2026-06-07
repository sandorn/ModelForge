namespace ModelForge.Sidecar.Configuration;

/// <summary>
/// Sidecar 配置选项，从 appsettings.json 的 "Sidecar" 节绑定。
/// 替代原 VSTO 项目中的 BridgeOptions，增加了 Sidecar 特有配置。
/// </summary>
public sealed class SidecarOptions
{
    /// <summary>后端桥接 API 基地址。</summary>
    public string BackendBaseUrl { get; set; } = "http://localhost:5095";

    /// <summary>Sidecar 本地 REST API 监听端口。</summary>
    public int SidecarPort { get; set; } = 5200;

    /// <summary>是否启用全局键盘钩子。</summary>
    public bool KeyboardHookEnabled { get; set; } = true;

    /// <summary>后端请求超时（秒）。</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Sidecar 调用 Backend 服务级端点的共享令牌；为空时禁用服务级读取。</summary>
    public string ServiceToken { get; set; } = string.Empty;

    /// <summary>保护 Sidecar 本地 /api/* 端点的共享令牌；为空时保持开发兼容模式。</summary>
    public string LocalApiToken { get; set; } = string.Empty;
}
