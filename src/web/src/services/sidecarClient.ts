/**
 * Sidecar 本地 REST API 客户端。
 * Office Web Add-in 任务窗格通过此客户端调用运行在本机的 Sidecar 服务。
 */

export interface SidecarHealth {
  status: string;
  service: string;
  timestampUtc: string;
}

export interface SidecarShortcut {
  commandId: string;
  displayName: string;
  shortcut: string;
}

export interface SidecarExecuteRequest {
  commandId: string;
  host: string;
  arguments?: Record<string, string>;
}

export interface SidecarExecuteResponse {
  success: boolean;
  commandId: string;
  message: string;
}

export interface SidecarExcelInfo {
  connected: boolean;
  workbook?: string;
  worksheet?: string;
  selection?: string;
  error?: string;
}

const SIDECAR_BASE_URL = import.meta.env.VITE_MODELFORGE_SIDECAR_URL || 'http://localhost:5200';

class SidecarClient {
  private baseUrl: string;

  constructor(baseUrl: string = SIDECAR_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  /** 健康检查。 */
  async health(): Promise<SidecarHealth> {
    const res = await fetch(`${this.baseUrl}/health`);
    if (!res.ok) throw new Error(`Sidecar health check failed: ${res.status}`);
    return res.json();
  }

  /** 获取所有已注册快捷键。 */
  async getShortcuts(): Promise<SidecarShortcut[]> {
    const res = await fetch(`${this.baseUrl}/api/shortcuts`);
    if (!res.ok) throw new Error(`Failed to fetch shortcuts: ${res.status}`);
    return res.json();
  }

  /** 执行命令（通过后端桥接分发）。 */
  async executeCommand(req: SidecarExecuteRequest): Promise<SidecarExecuteResponse> {
    const res = await fetch(`${this.baseUrl}/api/execute`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(req),
    });
    if (!res.ok) {
      const errorBody = await res.text();
      throw new Error(`Command execution failed: ${res.status} - ${errorBody}`);
    }
    return res.json();
  }

  /** 获取 Excel 运行状态（NetOffice 连接信息）。 */
  async getExcelInfo(): Promise<SidecarExcelInfo> {
    const res = await fetch(`${this.baseUrl}/api/excel/info`);
    if (!res.ok) throw new Error(`Failed to get Excel info: ${res.status}`);
    return res.json();
  }
}

export const sidecarClient = new SidecarClient();
