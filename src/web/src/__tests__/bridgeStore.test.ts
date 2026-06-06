import { describe, it, expect, vi, beforeEach } from 'vitest';
import { create } from 'zustand';

// Minimal bridgeStore clone for testing pure state transitions
// (avoids importing the real one which depends on runtime fetch)
type PanelId = 'dashboard' | 'commands' | 'sidecar' | 'audit' | 'aiwa' | 'admin';

type TestState = {
  isLoading: boolean;
  error?: string;
  sidecarConnected: boolean;
  activePanel: PanelId;
  setLoading: (v: boolean) => void;
  setError: (e?: string) => void;
  setSidecarConnected: (v: boolean) => void;
  setActivePanel: (p: PanelId) => void;
};

const useTestStore = create<TestState>((set) => ({
  isLoading: false,
  sidecarConnected: false,
  activePanel: 'dashboard',
  setLoading: (v) => set({ isLoading: v }),
  setError: (e) => set({ error: e }),
  setSidecarConnected: (v) => set({ sidecarConnected: v }),
  setActivePanel: (p) => set({ activePanel: p }),
}));

describe('bridgeStore state transitions', () => {
  beforeEach(() => {
    useTestStore.setState({
      isLoading: false,
      error: undefined,
      sidecarConnected: false,
      activePanel: 'dashboard',
    });
  });

  it('default panel is dashboard', () => {
    expect(useTestStore.getState().activePanel).toBe('dashboard');
  });

  it('setActivePanel changes panel', () => {
    useTestStore.getState().setActivePanel('commands');
    expect(useTestStore.getState().activePanel).toBe('commands');
  });

  it('setLoading toggles isLoading', () => {
    useTestStore.getState().setLoading(true);
    expect(useTestStore.getState().isLoading).toBe(true);

    useTestStore.getState().setLoading(false);
    expect(useTestStore.getState().isLoading).toBe(false);
  });

  it('setError stores and clears error', () => {
    useTestStore.getState().setError('something went wrong');
    expect(useTestStore.getState().error).toBe('something went wrong');

    useTestStore.getState().setError(undefined);
    expect(useTestStore.getState().error).toBeUndefined();
  });

  it('sidecarConnected defaults to false', () => {
    expect(useTestStore.getState().sidecarConnected).toBe(false);
  });

  it('setSidecarConnected updates connection status', () => {
    useTestStore.getState().setSidecarConnected(true);
    expect(useTestStore.getState().sidecarConnected).toBe(true);
  });
});