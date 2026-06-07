import { describe, expect, it } from 'vitest';
import { __adminConsoleTestables } from '../components/AdminConsole';

describe('AdminConsole API parsing', () => {
  it('unwraps ApiEnvelope responses', async () => {
    const response = new Response(JSON.stringify({
      traceId: 'trace-admin',
      data: [{ id: 'u1', username: 'admin', role: 'Admin', isActive: true }],
    }));

    const result = await __adminConsoleTestables.unwrapJson<Array<{ username: string }>>(response);

    expect(result[0].username).toBe('admin');
  });

  it('keeps raw JSON responses for legacy admin endpoints', async () => {
    const response = new Response(JSON.stringify([
      { id: 'u1', username: 'admin', role: 'Admin', isActive: true },
    ]));

    const result = await __adminConsoleTestables.unwrapJson<Array<{ username: string }>>(response);

    expect(result[0].username).toBe('admin');
  });

  it('throws envelope errors', async () => {
    const response = new Response(JSON.stringify({
      traceId: 'trace-admin',
      error: 'Access denied.',
    }));

    await expect(__adminConsoleTestables.unwrapJson(response)).rejects.toThrow('Access denied.');
  });

  it('unwraps role permission envelopes', async () => {
    const response = new Response(JSON.stringify({
      traceId: 'trace-roles',
      data: {
        roles: [
          { role: 'Admin', permissions: ['audit.view', 'users.manage'], builtIn: true },
        ],
      },
    }));

    const result = await __adminConsoleTestables.unwrapJson<{ roles: Array<{ role: string; permissions: string[] }> }>(response);

    expect(result.roles[0].role).toBe('Admin');
    expect(result.roles[0].permissions).toContain('users.manage');
  });
});

describe('AdminConsole audit timeline', () => {
  it('formats audit timeline bucket labels', () => {
    const label = __adminConsoleTestables.formatAuditTimelineLabel({
      startUtc: '2026-06-06T08:00:00Z',
      endUtc: '2026-06-06T09:00:00Z',
      count: 3,
    });

    expect(label).toContain('08:00-09:00');
  });

  it('normalizes audit filter queries for API calls', () => {
    const query = __adminConsoleTestables.buildAdminAuditQuery({
      count: 100,
      eventType: ' command.failed ',
      actorId: ' ',
      host: 1,
      severity: 2,
      search: ' Excel ',
    });

    expect(query).toEqual({
      count: 100,
      eventType: 'command.failed',
      host: 1,
      severity: 2,
      search: 'Excel',
    });
  });

  it('formats audit host and severity values', () => {
    expect(__adminConsoleTestables.parseAuditNumberFilter('2')).toBe(2);
    expect(__adminConsoleTestables.parseAuditNumberFilter('')).toBeUndefined();
    expect(__adminConsoleTestables.formatOfficeHost(1)).toBe('Excel');
    expect(__adminConsoleTestables.parseOfficeHostLabel('Excel')).toBe(1);
    expect(__adminConsoleTestables.parseOfficeHostLabel('powerpoint')).toBe(2);
    expect(__adminConsoleTestables.parseOfficeHostLabel('4')).toBe(4);
    expect(__adminConsoleTestables.parseOfficeHostLabel('unknown-host')).toBeUndefined();
    expect(__adminConsoleTestables.formatAuditSeverity(2)).toBe('Warning');
  });

  it('builds drilldown queries without dropping existing filters', () => {
    const query = __adminConsoleTestables.buildAdminAuditQuery({
      count: 100,
      search: 'failure',
      eventType: 'command.failed',
      host: __adminConsoleTestables.parseOfficeHostLabel('PowerPoint'),
    });

    expect(query).toEqual({
      count: 100,
      search: 'failure',
      eventType: 'command.failed',
      host: 2,
    });
  });

  it('validates audit retention day input', () => {
    expect(__adminConsoleTestables.parseAuditRetentionDays('90')).toBe(90);
    expect(__adminConsoleTestables.parseAuditRetentionDays('0')).toBeUndefined();
    expect(__adminConsoleTestables.parseAuditRetentionDays('3651')).toBeUndefined();
    expect(__adminConsoleTestables.parseAuditRetentionDays('1.5')).toBeUndefined();
  });
});

describe('AdminConsole dictionary highlighting', () => {
  it('returns one plain segment when there are no matches', () => {
    const result = __adminConsoleTestables.buildHighlightedTextSegments('No matches here.', []);

    expect(result).toEqual([{ text: 'No matches here.' }]);
  });

  it('splits text around ordered dictionary matches', () => {
    const result = __adminConsoleTestables.buildHighlightedTextSegments('Use EBITDA and TBD.', [
      {
        termId: 'term-1',
        term: 'EBITDA',
        matchedText: 'EBITDA',
        position: 4,
        suggestion: 'Adjusted EBITDA',
      },
      {
        termId: 'term-2',
        term: 'TBD',
        matchedText: 'TBD',
        position: 15,
      },
    ]);

    expect(result.map((segment) => segment.text)).toEqual(['Use ', 'EBITDA', ' and ', 'TBD', '.']);
    expect(result[1].match?.suggestion).toBe('Adjusted EBITDA');
    expect(result[3].match?.term).toBe('TBD');
  });

  it('sorts matches and skips invalid or overlapping ranges', () => {
    const result = __adminConsoleTestables.buildHighlightedTextSegments('ABC DEF GHI', [
      {
        termId: 'term-2',
        term: 'DEF',
        matchedText: 'DEF',
        position: 4,
      },
      {
        termId: 'term-overlap',
        term: 'BCD',
        matchedText: 'BCD',
        position: 1,
      },
      {
        termId: 'term-1',
        term: 'ABC',
        matchedText: 'ABC',
        position: 0,
      },
      {
        termId: 'term-invalid',
        term: 'overflow',
        matchedText: 'overflow',
        position: 99,
      },
    ]);

    expect(result.map((segment) => ({ text: segment.text, term: segment.match?.term }))).toEqual([
      { text: 'ABC', term: 'ABC' },
      { text: ' ', term: undefined },
      { text: 'DEF', term: 'DEF' },
      { text: ' GHI', term: undefined },
    ]);
  });
});

describe('AdminConsole dictionary import parsing', () => {
  it('parses dictionary export objects', () => {
    const result = __adminConsoleTestables.parseDictionaryImportPayload(JSON.stringify({
      terms: [
        { id: 'term-1', term: 'Custom Term', category: 'Custom', severity: 'Warning', updatedAt: '2026-06-06T00:00:00Z' },
      ],
    }));

    expect(result).toHaveLength(1);
    expect(result[0].term).toBe('Custom Term');
  });

  it('parses raw dictionary arrays', () => {
    const result = __adminConsoleTestables.parseDictionaryImportPayload(JSON.stringify([
      { id: 'term-1', term: 'Custom Term', category: 'Custom', severity: 'Warning', updatedAt: '2026-06-06T00:00:00Z' },
    ]));

    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('term-1');
  });

  it('rejects invalid dictionary import JSON shape', () => {
    expect(() => __adminConsoleTestables.parseDictionaryImportPayload(JSON.stringify({ items: [] })))
      .toThrow('terms array');
  });

  it('parses dictionary CSV templates', () => {
    const result = __adminConsoleTestables.parseDictionaryCsvPayload(
      'id,term,replacement,regexPattern,category,severity\r\n' +
      'term-1,"Revenue, net",Revenue,,Financial,Info',
    );

    expect(result).toHaveLength(1);
    expect(result[0].term).toBe('Revenue, net');
    expect(result[0].replacement).toBe('Revenue');
    expect(result[0].category).toBe('Financial');
  });

  it('formats dictionary CSV templates with escaped cells', () => {
    const csv = __adminConsoleTestables.formatDictionaryCsvPayload([
      {
        id: 'term-1',
        term: 'Revenue, net',
        replacement: 'Revenue "Net"',
        category: 'Financial',
        severity: 'Info',
        updatedAt: '2026-06-06T00:00:00Z',
      },
    ]);

    expect(csv).toContain('"Revenue, net"');
    expect(csv).toContain('"Revenue ""Net"""');
  });

  it('round-trips dictionary XLSX templates', async () => {
    const payload = await __adminConsoleTestables.formatDictionaryXlsxPayload([
      {
        id: 'term-1',
        term: 'Revenue, net',
        replacement: 'Revenue "Net"',
        regexPattern: 'Revenue\\s+net',
        category: 'Financial',
        severity: 'Info',
        updatedAt: '2026-06-06T00:00:00Z',
      },
    ]);

    const result = await __adminConsoleTestables.parseDictionaryXlsxPayload(payload);

    expect(result).toHaveLength(1);
    expect(result[0].term).toBe('Revenue, net');
    expect(result[0].replacement).toBe('Revenue "Net"');
    expect(result[0].regexPattern).toBe('Revenue\\s+net');
    expect(result[0].category).toBe('Financial');
    expect(result[0].severity).toBe('Info');
  });

  it('rejects dictionary CSV without term column', () => {
    expect(() => __adminConsoleTestables.parseDictionaryCsvPayload('id,name\r\n1,test'))
      .toThrow('term column');
  });

  it('rejects dictionary XLSX without term column', async () => {
    const payload = await __adminConsoleTestables.formatDictionaryXlsxPayload([]);

    await expect(__adminConsoleTestables.parseDictionaryXlsxPayload(payload))
      .rejects
      .toThrow('at least one term row');
  });
});
