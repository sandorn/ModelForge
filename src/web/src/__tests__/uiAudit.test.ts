import { afterEach, describe, expect, it, vi } from 'vitest';
import { buildUiAuditPayload, recordUiAction } from '../services/uiAudit';
import { useAuthStore } from '../services/authStore';

describe('uiAudit', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
    useAuthStore.setState({ token: null, user: null, isLoggedIn: false });
  });

  it('builds Web-hosted UI audit payloads with current user metadata', () => {
    useAuthStore.setState({
      token: 'token',
      user: { userId: 'u1', username: 'alice', role: 'Admin' },
      isLoggedIn: true,
    });

    const payload = buildUiAuditPayload({
      action: 'command.execute',
      commandId: 'excel.fill-down',
      metadata: { host: 'excel', count: 2, omitted: undefined },
    });

    expect(payload).toMatchObject({
      eventType: 'ui.command.execute',
      actorId: 'u1',
      host: 4,
      severity: 1,
      commandId: 'excel.fill-down',
    });
    expect(payload.metadata).toEqual({
      source: 'web-addin',
      username: 'alice',
      role: 'Admin',
      host: 'excel',
      count: '2',
    });
  });

  it('records UI audit events best effort without throwing', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => {
      throw new Error('offline');
    }));

    expect(() => recordUiAction({ action: 'nav.open', resourceId: 'dashboard' })).not.toThrow();
    await Promise.resolve();
  });
});
