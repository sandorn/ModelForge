import type { ApiEnvelope, CommandDefinition, HealthResponse, VersionInfoResponse } from '../types/contracts';
import { useAuthStore } from './authStore';

const DEFAULT_BACKEND_URL = import.meta.env.VITE_MODELFORGE_API_URL ?? 'http://localhost:5095';

export class ApiClient {
  constructor(private readonly baseUrl: string = DEFAULT_BACKEND_URL) {}

  getBackendBaseUrl() {
    return this.baseUrl;
  }

  async getHealth() {
    return this.get<HealthResponse>('/health');
  }

  async getVersion() {
    return this.get<VersionInfoResponse>('/api/version');
  }

  async getCommands() {
    return this.get<CommandDefinition[]>('/api/commands');
  }

  private async get<T>(path: string) {
    const headers = new Headers({
      'Accept': 'application/json',
      'X-Trace-Id': crypto.randomUUID(),
    });

    // 注入 JWT Token（如果已登录）
    const token = useAuthStore.getState().token;
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    const response = await fetch(`${this.baseUrl}${path}`, { headers });

    if (!response.ok) {
      throw new Error(`请求失败：${response.status} ${response.statusText}`);
    }

    const envelope = (await response.json()) as ApiEnvelope<T>;
    if (envelope.error) {
      throw new Error(envelope.error);
    }

    if (!envelope.data) {
      throw new Error('后端响应缺少 data 字段。');
    }

    return envelope.data;
  }
}

export const apiClient = new ApiClient();
