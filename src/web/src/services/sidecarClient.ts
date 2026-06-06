/**
 * Sidecar local REST API client.
 * Called by Office Web Add-in task panes to invoke COM operations
 * on the locally-running Sidecar service (:5200).
 */

import type { SidecarExecuteRequest, SidecarExecuteResponse, SidecarStatusResponse } from '../types/contracts';
import { useAuthStore } from './authStore';

export interface SidecarShortcut {
  commandId: string;
  displayName: string;
  shortcut: string;
}

const SIDECAR_BASE_URL = import.meta.env.VITE_MODELFORGE_SIDECAR_URL || 'http://localhost:5200';

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
  return headers;
}

class SidecarClient {
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

  /** POST /api/execute — runs a command via Sidecar COM interop */
  async executeCommand(req: SidecarExecuteRequest): Promise<SidecarExecuteResponse> {
    const res = await fetch(`${this.baseUrl}/api/execute`, {
      method: 'POST',
      headers: buildHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(req),
    });
    if (!res.ok) {
      const errorBody = await res.text();
      throw new Error(`Command execution failed: ${res.status} - ${errorBody.slice(0, 200)}`);
    }
    return res.json();
  }

  /** GET /api/excel/info — query Excel connection status and active document info */
  async getStatus(): Promise<SidecarStatusResponse> {
    const res = await fetch(`${this.baseUrl}/api/excel/info`, { headers: buildHeaders() });
    if (!res.ok) throw new Error(`Failed to get status: ${res.status}`);
    return res.json();
  }
}

export const sidecarClient = new SidecarClient();