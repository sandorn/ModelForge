import { create } from 'zustand';
import { apiClient } from './apiClient';
import { sidecarClient } from './sidecarClient';
import type { CommandDefinition, HealthResponse, VersionInfoResponse } from '../types/contracts';
import type { SidecarHealth, SidecarExcelInfo } from './sidecarClient';

export type PanelId = 'dashboard' | 'commands' | 'sidecar' | 'audit' | 'aiwa' | 'admin';

type BridgeState = {
  backendBaseUrl: string;
  health?: HealthResponse;
  version?: VersionInfoResponse;
  commands: CommandDefinition[];
  isLoading: boolean;
  error?: string;

  // Sidecar state
  sidecarHealth?: SidecarHealth;
  sidecarConnected: boolean;
  excelInfo?: SidecarExcelInfo;

  // Panel state
  activePanel: PanelId;

  refresh: () => Promise<void>;
  checkSidecar: () => Promise<void>;
  executeCommand: (commandId: string) => Promise<string>;
  setActivePanel: (panel: PanelId) => void;
};

export const useBridgeStore = create<BridgeState>((set, get) => ({
  backendBaseUrl: apiClient.getBackendBaseUrl(),
  commands: [],
  isLoading: false,
  sidecarConnected: false,
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
      let excelInfo: SidecarExcelInfo | undefined;
      try {
        excelInfo = await sidecarClient.getExcelInfo();
      } catch { /* Excel may not be connected */ }
      set({ sidecarHealth: health, sidecarConnected: true, excelInfo });
    } catch {
      set({ sidecarConnected: false, sidecarHealth: undefined, excelInfo: undefined });
    }
  },

  executeCommand: async (commandId: string) => {
    const result = await sidecarClient.executeCommand({ commandId, host: 'excel' });
    return result.message;
  },

  setActivePanel: (panel: PanelId) => set({ activePanel: panel }),
}));
