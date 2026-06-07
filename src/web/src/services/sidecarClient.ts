/**
 * Sidecar local REST API client.
 * Called by Office Web Add-in task panes to invoke COM operations
 * on the locally-running Sidecar service (:5200).
 */

import type {
  ApiEnvelope,
  ShortcutExportResponse,
  ShortcutImportRequest,
  ShortcutImportResponse,
  ShortcutItem,
  SidecarExecuteRequest,
  SidecarExecuteResponse,
  SidecarStatusResponse,
} from '../types/contracts';
import { useAuthStore } from './authStore';

export type SidecarShortcut = ShortcutItem;

const SIDECAR_BASE_URL = import.meta.env.VITE_MODELFORGE_SIDECAR_URL || 'http://localhost:5200';
const SIDECAR_TOKEN_HEADER = 'X-ModelForge-Sidecar-Token';

function getSidecarToken(): string | null {
  const envToken = import.meta.env.VITE_MODELFORGE_SIDECAR_TOKEN;
  if (typeof envToken === 'string' && envToken.trim()) {
    return envToken.trim();
  }

  const storedToken = localStorage.getItem('modelforge_sidecar_token');
  return storedToken?.trim() || null;
}

function buildHeaders(extra?: Record<string, string>): Headers {
  const headers = new Headers({
    Accept: 'application/json',
    'X-Trace-Id': crypto.randomUUID(),
    ...extra,
  });
  const token = useAuthStore.getState().token;
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  const sidecarToken = getSidecarToken();
  if (sidecarToken) {
    headers.set(SIDECAR_TOKEN_HEADER, sidecarToken);
  }
  return headers;
}

export class SidecarClient {
  private baseUrl: string;

  constructor(baseUrl: string = SIDECAR_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  /** GET /health */
  async health(): Promise<{ status: string; service: string; timestampUtc: string }> {
    const res = await fetch(`${this.baseUrl}/health`, { headers: buildHeaders() });
    if (!res.ok) throw new Error(`Sidecar health check failed: ${res.status}`);
    return res.json();
  }

  /** GET /api/shortcuts */
  async getShortcuts(): Promise<SidecarShortcut[]> {
    const res = await fetch(`${this.baseUrl}/api/shortcuts`, { headers: buildHeaders() });
    if (!res.ok) throw new Error(`Failed to fetch shortcuts: ${res.status}`);
    return res.json();
  }

  /** GET /api/shortcuts/export */
  async exportShortcuts(): Promise<ShortcutExportResponse> {
    const res = await fetch(`${this.baseUrl}/api/shortcuts/export`, { headers: buildHeaders() });
    return this.unwrapEnvelope<ShortcutExportResponse>(res, 'Sidecar returned an empty shortcut export response.');
  }

  /** POST /api/shortcuts/import */
  async importShortcuts(req: ShortcutImportRequest): Promise<ShortcutImportResponse> {
    const res = await fetch(`${this.baseUrl}/api/shortcuts/import`, {
      method: 'POST',
      headers: buildHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(req),
    });
    return this.unwrapEnvelope<ShortcutImportResponse>(res, 'Sidecar returned an empty shortcut import response.');
  }

  /** POST /api/execute — runs a command via Sidecar COM interop */
  async executeCommand(req: SidecarExecuteRequest): Promise<SidecarExecuteResponse> {
    const res = await fetch(`${this.baseUrl}/api/execute`, {
      method: 'POST',
      headers: buildHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(req),
    });

    const envelope = (await res.json()) as ApiEnvelope<SidecarExecuteResponse>;
    if (!res.ok || envelope.error) {
      const message = envelope.error || envelope.data?.message || `Command execution failed: ${res.status}`;
      throw new Error(message);
    }
    if (!envelope.data) {
      throw new Error('Sidecar returned an empty execution response.');
    }
    return envelope.data;
  }

  /** GET /api/status — query Office connection status and active document info */
  async getStatus(): Promise<SidecarStatusResponse> {
    const res = await fetch(`${this.baseUrl}/api/status`, { headers: buildHeaders() });
    return this.unwrapEnvelope<SidecarStatusResponse>(res, 'Sidecar returned an empty status response.');
  }

  private async unwrapEnvelope<T>(res: Response, emptyMessage: string): Promise<T> {
    if (!res.ok) {
      const text = await res.text();
      try {
        const envelope = JSON.parse(text) as ApiEnvelope<T>;
        throw new Error(envelope.error || `Sidecar request failed: ${res.status}`);
      } catch (error) {
        if (error instanceof SyntaxError) {
          throw new Error(`Sidecar request failed: ${res.status}`);
        }
        throw error;
      }
    }

    const envelope = (await res.json()) as ApiEnvelope<T>;
    if (envelope.error) {
      throw new Error(envelope.error);
    }
    if (!envelope.data) {
      throw new Error(emptyMessage);
    }
    return envelope.data;
  }
}

export const sidecarClient = new SidecarClient();
