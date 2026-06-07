import { afterEach, describe, expect, it, vi } from 'vitest';
import { SidecarClient } from '../services/sidecarClient';

describe('SidecarClient shortcut APIs', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('unwraps shortcut export envelopes', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-shortcuts',
      data: {
        shortcuts: [
          { commandId: 'excel.fill-right', displayName: '快速向右填充', shortcut: 'Ctrl+Alt+R' },
        ],
        count: 1,
        exportedAtUtc: '2026-06-06T00:00:00Z',
      },
    }))));

    const client = new SidecarClient('http://localhost:5200');
    const result = await client.exportShortcuts();

    expect(result.count).toBe(1);
    expect(result.shortcuts[0].commandId).toBe('excel.fill-right');
  });

  it('posts shortcut import envelopes', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      expect(init?.method).toBe('POST');
      expect(JSON.parse(String(init?.body))).toEqual({
        shortcuts: [
          { commandId: 'excel.fill-down', displayName: '快速向下填充', shortcut: 'Ctrl+Alt+D' },
        ],
      });

      return new Response(JSON.stringify({
        traceId: 'trace-shortcuts',
        data: {
          imported: 1,
          shortcuts: [
            { commandId: 'excel.fill-down', displayName: '快速向下填充', shortcut: 'Ctrl+Alt+D' },
          ],
        },
      }));
    });
    vi.stubGlobal('fetch', fetchMock);

    const client = new SidecarClient('http://localhost:5200');
    const result = await client.importShortcuts({
      shortcuts: [
        { commandId: 'excel.fill-down', displayName: '快速向下填充', shortcut: 'Ctrl+Alt+D' },
      ],
    });

    expect(result.imported).toBe(1);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5200/api/shortcuts/import',
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('throws shortcut import envelope errors', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-shortcuts',
      error: '快捷键冲突',
    }), { status: 400 })));

    const client = new SidecarClient('http://localhost:5200');

    await expect(client.importShortcuts({ shortcuts: [] })).rejects.toThrow('快捷键冲突');
  });

  it('adds local Sidecar token header when configured in localStorage', async () => {
    localStorage.setItem('modelforge_sidecar_token', 'local-sidecar-token');
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      expect((init?.headers as Headers).get('X-ModelForge-Sidecar-Token')).toBe('local-sidecar-token');
      return new Response(JSON.stringify({
        traceId: 'trace-shortcuts',
        data: {
          shortcuts: [],
          count: 0,
          exportedAtUtc: '2026-06-06T00:00:00Z',
        },
      }));
    });
    vi.stubGlobal('fetch', fetchMock);

    const client = new SidecarClient('http://localhost:5200');
    await client.exportShortcuts();

    expect(fetchMock).toHaveBeenCalledOnce();
  });
});
