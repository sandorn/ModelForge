import { create } from 'zustand';
import { apiClient } from './apiClient';
import { sidecarClient } from './sidecarClient';
import { recordUiAction } from './uiAudit';
import type { CommandDefinition, HealthResponse, VersionInfoResponse } from '../types/contracts';
import type { SidecarStatusResponse } from '../types/contracts';

export type PanelId = 'dashboard' | 'commands' | 'sidecar' | 'audit' | 'aiwa' | 'admin' | 'links' | 'ppt' | 'word' | 'templates' | 'shortcuts' | 'aiConfig' | 'settings';

type BridgeState = {
  backendBaseUrl: string;
  health?: HealthResponse;
  version?: VersionInfoResponse;
  commands: CommandDefinition[];
  isLoading: boolean;
  error?: string;

  // Sidecar state
  sidecarHealth?: HealthResponse;
  sidecarConnected: boolean;
  excelInfo?: SidecarStatusResponse;

  // Host detection
  currentHost: 'excel' | 'powerpoint' | 'word' | '';

  // Panel state
  activePanel: PanelId;

  refresh: () => Promise<void>;
  checkSidecar: () => Promise<void>;
  executeCommand: (commandId: string, host?: string) => Promise<string>;
  setActivePanel: (panel: PanelId) => void;
};

function detectOfficeHost(): 'excel' | 'powerpoint' | 'word' | '' {
  try {
    const host = (window as any).Office?.context?.host;
    if (host === (window as any).Office?.HostType?.Excel) return 'excel';
    if (host === (window as any).Office?.HostType?.PowerPoint) return 'powerpoint';
    if (host === (window as any).Office?.HostType?.Word) return 'word';
  } catch { }
  return '';
}

export const useBridgeStore = create<BridgeState>((set, get) => ({
  backendBaseUrl: apiClient.getBackendBaseUrl(),
  commands: [],
  isLoading: false,
  sidecarConnected: false,
  currentHost: detectOfficeHost(),
  activePanel: 'dashboard',

  refresh: async () => {
    set({ isLoading: true, error: undefined });
    try {
      const [health, version, commands] = await Promise.all([
        apiClient.getHealth(),
        apiClient.getVersion(),
        apiClient.getCommands()
      ]);
      set({ health, version, commands, isLoading: false });
    } catch (error) {
      set({
        error: error instanceof Error ? error.message : '未知错误',
        isLoading: false
      });
    }
  },

  checkSidecar: async () => {
    try {
      const health = await sidecarClient.health();
      let excelInfo: SidecarStatusResponse | undefined;
      try {
        excelInfo = await sidecarClient.getStatus();
      } catch { /* Excel may not be connected */ }
      set({ sidecarHealth: health, sidecarConnected: true, excelInfo });
    } catch {
      set({ sidecarConnected: false, sidecarHealth: undefined, excelInfo: undefined });
    }
  },

  executeCommand: async (commandId: string, host?: string) => {
    const hostStr = host ?? 'excel';
    recordUiAction({
      action: 'command.execute',
      commandId,
      metadata: { host: hostStr },
    });
    const result = await sidecarClient.executeCommand({ commandId, host: hostStr });
    return result.message;
  },

  setActivePanel: (panel: PanelId) => set({ activePanel: panel }),
}));
