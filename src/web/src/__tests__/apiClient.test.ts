import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiClient } from '../services/apiClient';

describe('ApiClient', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('unwraps login ApiEnvelope responses', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-login',
      data: {
        token: 'jwt-token',
        userId: 'u1',
        username: 'admin',
        role: 'Admin',
        expiresAt: '2026-06-06T10:00:00Z',
      },
    }))));

    const client = new ApiClient('http://localhost:5095');
    const result = await client.login('admin', 'admin123');

    expect(result.token).toBe('jwt-token');
    expect(result.username).toBe('admin');
  });

  it('throws login envelope errors', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-login',
      error: '登录失败。',
    }))));

    const client = new ApiClient('http://localhost:5095');

    await expect(client.login('admin', 'bad-password')).rejects.toThrow('登录失败。');
  });

  it('calls dictionary import and export envelope endpoints', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/api/dictionary/export')) {
        return new Response(JSON.stringify({
          traceId: 'trace-export',
          data: {
            terms: [{ id: 'term-1', term: '术语', category: 'Custom', severity: 'Warning', updatedAt: '2026-06-06T00:00:00Z' }],
            count: 1,
            exportedAtUtc: '2026-06-06T00:00:00Z',
          },
        }));
      }

      expect(url).toBe('http://localhost:5095/api/dictionary/import');
      expect(init?.method).toBe('POST');
      expect(JSON.parse(String(init?.body))).toEqual({
        terms: [{ id: 'term-1', term: '术语', category: 'Custom', severity: 'Warning', updatedAt: '2026-06-06T00:00:00Z' }],
        overwrite: true,
      });

      return new Response(JSON.stringify({
        traceId: 'trace-import',
        data: {
          imported: 1,
          skipped: 0,
          errors: [],
          terms: [{ id: 'term-1', term: '术语', category: 'Custom', severity: 'Warning', updatedAt: '2026-06-06T00:00:00Z' }],
        },
      }));
    });
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    const exported = await client.exportDictionaryTerms();
    const imported = await client.importDictionaryTerms({ terms: exported.terms, overwrite: true });

    expect(exported.count).toBe(1);
    expect(imported.imported).toBe(1);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('gets admin diagnostics from the envelope endpoint', async () => {
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-diagnostics',
      data: {
        generatedAtUtc: '2026-06-06T00:00:00Z',
        version: {
          product: 'ModelForge',
          component: 'Backend',
          version: '0.1.3',
          apiVersion: 'v1',
          buildTimestampUtc: '2026-06-06T00:00:00Z',
        },
        databaseProvider: 'sqlite',
        databaseConnected: true,
        commandCount: 120,
        linkCount: 3,
        dictionaryTermCount: 5,
        recentAuditEventCount: 8,
        auditRetentionDays: 90,
        auditRetentionCutoffUtc: '2026-03-08T00:00:00Z',
        auditEventsEligibleForRetentionPrune: 0,
        configuration: { FeatureFlags: 'enabled' },
        notes: ['No secrets included'],
      },
    })));
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    const diagnostics = await client.getAdminDiagnostics();

    expect(diagnostics.databaseConnected).toBe(true);
    expect(diagnostics.commandCount).toBe(120);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5095/api/admin/diagnostics',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
  });

  it('gets admin roles from the envelope endpoint', async () => {
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-roles',
      data: {
        roles: [
          { role: 'Admin', permissions: ['audit.view', 'users.manage'], builtIn: true },
        ],
      },
    })));
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    const roles = await client.getAdminRoles();

    expect(roles.roles[0].role).toBe('Admin');
    expect(roles.roles[0].permissions).toContain('users.manage');
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5095/api/admin/roles',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
  });

  it('gets admin audit summaries from the envelope endpoint', async () => {
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({
      traceId: 'trace-audit-summary',
      data: {
        generatedAtUtc: '2026-06-06T00:00:00Z',
        windowHours: 168,
        bucketHours: 24,
        totalEvents: 2,
        byEventType: [{ key: 'command.executed', count: 2 }],
        byHost: [{ key: 'Excel', count: 2 }],
        byActor: [{ key: 'admin', count: 2 }],
        timeline: [{ startUtc: '2026-06-06T00:00:00Z', endUtc: '2026-06-07T00:00:00Z', count: 2 }],
        heatmap: [{ rowKey: 'command.executed', columnKey: 'Excel', count: 2 }],
      },
    })));
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    const summary = await client.getAdminAuditSummary();

    expect(summary.totalEvents).toBe(2);
    expect(summary.bucketHours).toBe(24);
    expect(summary.byEventType[0].key).toBe('command.executed');
    expect(summary.timeline[0].count).toBe(2);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5095/api/admin/audit-events/summary?hours=168',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
  });

  it('passes admin audit filter queries to list, summary, and export endpoints', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes('/summary')) {
        return new Response(JSON.stringify({
          traceId: 'trace-audit-summary',
          data: {
            generatedAtUtc: '2026-06-06T00:00:00Z',
            windowHours: 24,
            bucketHours: 1,
            totalEvents: 1,
            byEventType: [{ key: 'command.failed', count: 1 }],
            byHost: [{ key: 'Excel', count: 1 }],
            byActor: [{ key: 'alice', count: 1 }],
            timeline: [{ startUtc: '2026-06-06T00:00:00Z', endUtc: '2026-06-06T01:00:00Z', count: 1 }],
            heatmap: [{ rowKey: 'command.failed', columnKey: 'Excel', count: 1 }],
            query: { actorId: 'alice', host: 1 },
          },
        }));
      }

      if (url.includes('/export')) {
        return new Response('eventId,recordedAtUtc,eventType\r\n', {
          headers: { 'Content-Type': 'text/csv' },
        });
      }

      return new Response(JSON.stringify({
        traceId: 'trace-audit-list',
        data: {
          items: [],
          pagination: { page: 1, pageSize: 100, total: 0 },
          query: { actorId: 'alice', host: 1 },
        },
      }));
    });
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    await client.getAdminAuditEvents({ count: 100, actorId: 'alice', host: 1, severity: 2, search: 'failed' });
    await client.getAdminAuditSummary(24, { actorId: 'alice', host: 1 });
    await client.downloadAdminAuditCsv({ count: 500, actorId: 'alice', host: 1 });

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      'http://localhost:5095/api/admin/audit-events?count=100&actorId=alice&host=1&severity=2&search=failed',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      'http://localhost:5095/api/admin/audit-events/summary?actorId=alice&host=1&hours=24',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      3,
      'http://localhost:5095/api/admin/audit-events/export?count=500&actorId=alice&host=1',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
  });

  it('applies admin audit retention policy through envelope endpoint', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      expect(String(input)).toBe('http://localhost:5095/api/admin/audit-events/retention');
      expect(init?.method).toBe('POST');
      expect(JSON.parse(String(init?.body))).toEqual({ retentionDays: 90, dryRun: true });

      return new Response(JSON.stringify({
        traceId: 'trace-retention',
        data: {
          retentionDays: 90,
          cutoffUtc: '2026-03-08T00:00:00Z',
          matchedEvents: 3,
          deletedEvents: 0,
          dryRun: true,
          executedAtUtc: '2026-06-06T00:00:00Z',
        },
      }));
    });
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    const result = await client.applyAdminAuditRetention({ retentionDays: 90, dryRun: true });

    expect(result.matchedEvents).toBe(3);
    expect(result.deletedEvents).toBe(0);
  });

  it('downloads the admin diagnostics bundle as a blob', async () => {
    const fetchMock = vi.fn(async () => new Response(
      JSON.stringify({ summary: { commandCount: 1 }, runtime: {}, recentAuditEvents: [], notes: [] }),
      { headers: { 'Content-Type': 'application/json' } },
    ));
    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5095');
    const blob = await client.downloadAdminDiagnosticsBundle();

    expect(blob.type).toBe('application/json');
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5095/api/admin/diagnostics/bundle',
      expect.objectContaining({ headers: expect.any(Headers) }),
    );
  });
});
