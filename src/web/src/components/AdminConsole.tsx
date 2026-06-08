import { useEffect, useMemo, useState } from 'react';
import {
  Badge,
  Button,
  Card,
  CardHeader,
  Input,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
  Textarea,
  Title3,
} from '@fluentui/react-components';
import { apiClient } from '../services/apiClient';
import { recordUiAction } from '../services/uiAudit';
import { AuditSeverity, OfficeHost } from '../types/contracts';
import type {
  AdminAuditEventItem,
  AdminAuditEventsQuery,
  AdminAuditEventsResponse,
  AdminAuditHeatmapCell,
  AdminAuditRetentionResponse,
  AdminAuditSummaryResponse,
  AdminAuditTimelineBucket,
  AdminDiagnosticsResponse,
  AdminRolePermissionResponse,
  AdminUserResponse,
  ApiEnvelope,
  DictionaryCheckResponse,
  DictionaryTerm,
  TermMatch,
} from '../types/contracts';

type AdminTab = 'users' | 'roles' | 'audit' | 'config' | 'dictionary' | 'diagnostics';
type AuditRow = AdminAuditEventItem;
type HighlightedTextSegment = {
  text: string;
  match?: TermMatch;
};
type DictionaryTableCell = string | number | boolean | Date | null | undefined;

async function unwrapJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  const payload = await response.json();
  if (payload && typeof payload === 'object' && ('data' in payload || 'error' in payload)) {
    const envelope = payload as ApiEnvelope<T>;
    if (envelope.error) {
      throw new Error(envelope.error);
    }
    if (envelope.data === undefined) {
      throw new Error('Response missing data field.');
    }
    return envelope.data;
  }

  return payload as T;
}

function buildHighlightedTextSegments(text: string, matches: TermMatch[]): HighlightedTextSegment[] {
  if (!text || matches.length === 0) {
    return [{ text }];
  }

  const segments: HighlightedTextSegment[] = [];
  let cursor = 0;
  const orderedMatches = [...matches].sort((left, right) => left.position - right.position);

  for (const match of orderedMatches) {
    const start = match.position;
    const end = start + match.matchedText.length;
    if (start < cursor || start < 0 || end > text.length || end <= start) {
      continue;
    }

    if (start > cursor) {
      segments.push({ text: text.slice(cursor, start) });
    }

    segments.push({ text: text.slice(start, end), match });
    cursor = end;
  }

  if (cursor < text.length) {
    segments.push({ text: text.slice(cursor) });
  }

  return segments.length > 0 ? segments : [{ text }];
}

function parseDictionaryImportPayload(payloadText: string): DictionaryTerm[] {
  const payload = JSON.parse(payloadText) as { terms?: DictionaryTerm[] } | DictionaryTerm[];
  const terms = Array.isArray(payload) ? payload : payload.terms;
  if (!Array.isArray(terms)) {
    throw new Error('Dictionary import payload must be an array or an object with a terms array.');
  }

  return terms;
}

function parseDictionaryCsvPayload(payloadText: string): DictionaryTerm[] {
  return parseDictionaryTableRows(parseCsvRows(payloadText), 'CSV');
}

async function parseDictionaryXlsxPayload(payload: ArrayBuffer): Promise<DictionaryTerm[]> {
  const zip = await loadXlsxZip(payload);
  const workbookXml = readZipText(zip, 'xl/workbook.xml');
  const workbookRelsXml = readZipText(zip, 'xl/_rels/workbook.xml.rels');
  const workbookDoc = parseXmlDocument(workbookXml);
  const workbookRelsDoc = parseXmlDocument(workbookRelsXml);
  const firstSheet = workbookDoc.getElementsByTagName('sheet')[0];
  const relationshipId = firstSheet?.getAttribute('r:id');
  if (!relationshipId) {
    throw new Error('XLSX file must contain at least one worksheet.');
  }

  const relationship = Array.from(workbookRelsDoc.getElementsByTagName('Relationship'))
    .find((item) => item.getAttribute('Id') === relationshipId);
  const sheetTarget = relationship?.getAttribute('Target');
  if (!sheetTarget) {
    throw new Error('XLSX workbook relationship is missing.');
  }

  const sheetPath = normalizeXlsxPath('xl', sheetTarget);
  const sharedStrings = zip['xl/sharedStrings.xml']
    ? parseSharedStrings(readZipText(zip, 'xl/sharedStrings.xml'))
    : [];
  const rows = parseWorksheetRows(readZipText(zip, sheetPath), sharedStrings);
  return parseDictionaryTableRows(rows, 'XLSX');
}

function parseDictionaryTableRows(rows: DictionaryTableCell[][], sourceName: 'CSV' | 'XLSX'): DictionaryTerm[] {
  if (rows.length < 2) {
    throw new Error(`${sourceName} file must contain a header row and at least one term row.`);
  }

  const header = rows[0].map((cell) => normalizeDictionaryCell(cell).trim());
  const indexOf = (name: string) => header.findIndex((column) => column.toLowerCase() === name.toLowerCase());
  const termIndex = indexOf('term');
  if (termIndex < 0) {
    throw new Error(`${sourceName} header must contain a term column.`);
  }

  const idIndex = indexOf('id');
  const replacementIndex = indexOf('replacement');
  const regexPatternIndex = indexOf('regexPattern');
  const categoryIndex = indexOf('category');
  const severityIndex = indexOf('severity');

  return rows.slice(1)
    .filter((row) => row.some((cell) => normalizeDictionaryCell(cell).trim()))
    .map((row) => ({
      id: idIndex >= 0 ? normalizeDictionaryCell(row[idIndex]).trim() : '',
      term: normalizeDictionaryCell(row[termIndex]).trim(),
      replacement: replacementIndex >= 0 ? normalizeDictionaryCell(row[replacementIndex]).trim() || undefined : undefined,
      regexPattern: regexPatternIndex >= 0 ? normalizeDictionaryCell(row[regexPatternIndex]).trim() || undefined : undefined,
      category: categoryIndex >= 0 ? normalizeDictionaryCell(row[categoryIndex]).trim() || 'General' : 'General',
      severity: severityIndex >= 0 ? normalizeDictionaryCell(row[severityIndex]).trim() || 'Warning' : 'Warning',
      updatedAt: new Date().toISOString(),
    }));
}

function formatDictionaryCsvPayload(terms: DictionaryTerm[]): string {
  const rows = [
    ['id', 'term', 'replacement', 'regexPattern', 'category', 'severity'],
    ...terms.map((term) => [
      term.id,
      term.term,
      term.replacement ?? '',
      term.regexPattern ?? '',
      term.category,
      term.severity,
    ]),
  ];

  return rows.map((row) => row.map(escapeCsvCell).join(',')).join('\r\n');
}

async function formatDictionaryXlsxPayload(terms: DictionaryTerm[]): Promise<ArrayBuffer> {
  const { strToU8, zipSync } = await import('fflate');
  const rows = [
    ['id', 'term', 'replacement', 'regexPattern', 'category', 'severity'],
    ...terms.map((term) => [
      term.id,
      term.term,
      term.replacement ?? '',
      term.regexPattern ?? '',
      term.category,
      term.severity,
    ]),
  ];
  const files: Record<string, Uint8Array> = {
    '[Content_Types].xml': strToU8(xmlDeclaration([
      '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">',
      '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>',
      '<Default Extension="xml" ContentType="application/xml"/>',
      '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>',
      '<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>',
      '</Types>',
    ].join(''))),
    '_rels/.rels': strToU8(xmlDeclaration([
      '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">',
      '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>',
      '</Relationships>',
    ].join(''))),
    'xl/workbook.xml': strToU8(xmlDeclaration([
      '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" ',
      'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">',
      '<sheets><sheet name="Dictionary" sheetId="1" r:id="rId1"/></sheets>',
      '</workbook>',
    ].join(''))),
    'xl/_rels/workbook.xml.rels': strToU8(xmlDeclaration([
      '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">',
      '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>',
      '</Relationships>',
    ].join(''))),
    'xl/worksheets/sheet1.xml': strToU8(buildWorksheetXml(rows)),
  };

  return zipSync(files, { level: 6 }).slice().buffer as ArrayBuffer;
}

function parseCsvRows(payloadText: string): string[][] {
  const rows: string[][] = [];
  let currentRow: string[] = [];
  let currentCell = '';
  let inQuotes = false;

  for (let index = 0; index < payloadText.length; index++) {
    const char = payloadText[index];
    const nextChar = payloadText[index + 1];

    if (char === '"') {
      if (inQuotes && nextChar === '"') {
        currentCell += '"';
        index++;
      } else {
        inQuotes = !inQuotes;
      }
      continue;
    }

    if (char === ',' && !inQuotes) {
      currentRow.push(currentCell);
      currentCell = '';
      continue;
    }

    if ((char === '\n' || char === '\r') && !inQuotes) {
      if (char === '\r' && nextChar === '\n') {
        index++;
      }
      currentRow.push(currentCell);
      rows.push(currentRow);
      currentRow = [];
      currentCell = '';
      continue;
    }

    currentCell += char;
  }

  currentRow.push(currentCell);
  if (currentRow.some((cell) => cell.length > 0)) {
    rows.push(currentRow);
  }

  return rows;
}

function escapeCsvCell(value: string): string {
  return /[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}

function normalizeDictionaryCell(value: DictionaryTableCell): string {
  if (value === null || value === undefined) {
    return '';
  }
  return value instanceof Date ? value.toISOString() : String(value);
}

async function loadXlsxZip(payload: ArrayBuffer): Promise<Record<string, Uint8Array>> {
  const { unzipSync } = await import('fflate');
  return unzipSync(new Uint8Array(payload));
}

function readZipText(zip: Record<string, Uint8Array>, path: string): string {
  const file = zip[path];
  if (!file) {
    throw new Error(`XLSX file is missing ${path}.`);
  }

  return new TextDecoder().decode(file);
}

function parseXmlDocument(xml: string): Document {
  const document = new DOMParser().parseFromString(xml, 'application/xml');
  if (document.getElementsByTagName('parsererror').length > 0) {
    throw new Error('XLSX file contains invalid XML.');
  }

  return document;
}

function normalizeXlsxPath(basePath: string, target: string): string {
  const normalizedTarget = target.replace(/\\/g, '/');
  const path = normalizedTarget.startsWith('/')
    ? normalizedTarget.slice(1)
    : `${basePath}/${normalizedTarget}`;
  const segments: string[] = [];
  for (const segment of path.split('/')) {
    if (!segment || segment === '.') {
      continue;
    }
    if (segment === '..') {
      segments.pop();
    } else {
      segments.push(segment);
    }
  }

  return segments.join('/');
}

function parseSharedStrings(xml: string): string[] {
  const document = parseXmlDocument(xml);
  return Array.from(document.getElementsByTagName('si')).map((item) =>
    Array.from(item.getElementsByTagName('t')).map((node) => node.textContent ?? '').join(''));
}

function parseWorksheetRows(xml: string, sharedStrings: string[]): string[][] {
  const document = parseXmlDocument(xml);
  return Array.from(document.getElementsByTagName('row')).map((row) => {
    const values: string[] = [];
    for (const cell of Array.from(row.getElementsByTagName('c'))) {
      const reference = cell.getAttribute('r');
      const columnIndex = reference ? getColumnIndex(reference) : values.length;
      values[columnIndex] = readWorksheetCell(cell, sharedStrings);
    }
    return values.map((value) => value ?? '');
  });
}

function readWorksheetCell(cell: Element, sharedStrings: string[]): string {
  if (cell.getAttribute('t') === 's') {
    const index = Number(cell.getElementsByTagName('v')[0]?.textContent ?? '-1');
    return Number.isInteger(index) && index >= 0 ? sharedStrings[index] ?? '' : '';
  }

  if (cell.getAttribute('t') === 'inlineStr') {
    return Array.from(cell.getElementsByTagName('t')).map((node) => node.textContent ?? '').join('');
  }

  return cell.getElementsByTagName('v')[0]?.textContent ?? '';
}

function buildWorksheetXml(rows: string[][]): string {
  const sheetData = rows.map((row, rowIndex) => {
    const rowNumber = rowIndex + 1;
    const cells = row.map((value, columnIndex) => {
      const reference = `${getColumnName(columnIndex)}${rowNumber}`;
      return `<c r="${reference}" t="inlineStr"><is><t>${escapeXml(value)}</t></is></c>`;
    }).join('');
    return `<row r="${rowNumber}">${cells}</row>`;
  }).join('');

  return xmlDeclaration(`<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>${sheetData}</sheetData></worksheet>`);
}

function getColumnIndex(reference: string): number {
  const letters = reference.replace(/\d/g, '').toUpperCase();
  return letters.split('').reduce((acc, char) => acc * 26 + (char.charCodeAt(0) - 64), 0) - 1;
}

function getColumnName(index: number): string {
  let value = index + 1;
  let name = '';
  while (value > 0) {
    const remainder = (value - 1) % 26;
    name = String.fromCharCode(65 + remainder) + name;
    value = Math.floor((value - 1) / 26);
  }

  return name;
}

function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function xmlDeclaration(xml: string): string {
  return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>${xml}`;
}

export function AdminConsole() {
  const [activeTab, setActiveTab] = useState<AdminTab>('users');
  const tabs: AdminTab[] = ['users', 'roles', 'audit', 'config', 'dictionary', 'diagnostics'];

  return (
    <div className="panel">
      <Title3>管理控制台</Title3>

      <div className="admin-tabs">
        {tabs.map((tab) => (
          <button
            key={tab}
            className={`admin-tab${activeTab === tab ? ' active' : ''}`}
            onClick={() => {
              recordUiAction({ action: 'admin.tab.open', resourceId: tab });
              setActiveTab(tab);
            }}
          >
            {tab === 'users'
              ? '用户管理'
              : tab === 'roles'
                ? '角色权限'
                : tab === 'audit'
                  ? '审计日志'
                  : tab === 'config'
                    ? '配置'
                    : tab === 'dictionary'
                      ? '企业词典'
                      : '诊断'}
          </button>
        ))}
      </div>

      {activeTab === 'users' && <UserManagement />}
      {activeTab === 'roles' && <RolePermissions />}
      {activeTab === 'audit' && <AuditLog />}
      {activeTab === 'config' && <ConfigPanel />}
      {activeTab === 'dictionary' && <DictionaryPanel />}
      {activeTab === 'diagnostics' && <DiagnosticsPanel />}
    </div>
  );
}

function UserManagement() {
  const [users, setUsers] = useState<AdminUserResponse[]>([]);
  const [newName, setNewName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [editingUser, setEditingUser] = useState<AdminUserResponse | null>(null);
  const [editRole, setEditRole] = useState('Analyst');

  const loadUsers = async () => {
    setLoading(true);
    setError(null);
    try {
      setUsers(await apiClient.getAdminUsers());
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载用户失败。');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  const addUser = async () => {
    if (!newName.trim()) {
      return;
    }
    setError(null);
    try {
      await apiClient.createAdminUser({ username: newName.trim(), password: 'ChangeMe123!', role: 'Analyst' });
      setNewName('');
      await loadUsers();
    } catch (err) {
      setError(err instanceof Error ? err.message : '创建用户失败。');
    }
  };

  const toggleStatus = async (id: string) => {
    setError(null);
    try {
      await apiClient.toggleAdminUser(id);
      await loadUsers();
    } catch (err) {
      setError(err instanceof Error ? err.message : '切换用户状态失败。');
    }
  };

  const deleteUser = async (id: string) => {
    setError(null);
    try { await apiClient.deleteAdminUser(id); await loadUsers(); }
    catch (err) { setError(err instanceof Error ? err.message : '删除用户失败。'); }
  };

  const updateUser = async () => {
    if (!editingUser) return;
    setError(null);
    try { await apiClient.updateAdminUser(editingUser.id, { role: editRole }); setEditingUser(null); await loadUsers(); }
    catch (err) { setError(err instanceof Error ? err.message : '更新用户失败。'); }
  };

  if (loading) return <Spinner label="正在加载用户..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">用户列表</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        <Input placeholder="用户名" value={newName} onChange={(_, data) => setNewName(data.value)} />
        <Button appearance="primary" onClick={addUser}>添加用户</Button>
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>用户名</TableHeaderCell>
            <TableHeaderCell>角色</TableHeaderCell>
            <TableHeaderCell>状态</TableHeaderCell>
            <TableHeaderCell>创建时间</TableHeaderCell>
            <TableHeaderCell>操作</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell><Text weight="semibold">{user.username}</Text></TableCell>
              <TableCell>{user.role}</TableCell>
              <TableCell><Badge appearance="outline">{user.isActive ? '启用' : '禁用'}</Badge></TableCell>
              <TableCell><Text size={100}>{formatDate(user.createdAt)}</Text></TableCell>
              <TableCell>
                <div style={{ display: 'flex', gap: '4px' }}>
                  <Button size="small" onClick={() => void toggleStatus(user.id)}>切换</Button>
                  <Button size="small" onClick={() => { setEditingUser(user); setEditRole(user.role); }}>编辑</Button>
                  <Button size="small" onClick={() => { if (confirm(`确认删除用户 ${user.username}?`)) void deleteUser(user.id); }}>删除</Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {editingUser && (
        <div className="omnibar-overlay" onClick={() => setEditingUser(null)}>
          <Card className="omnibar-modal" onClick={e => e.stopPropagation()} style={{ padding: '1rem', maxWidth: '360px' }}>
            <CardHeader header={<Text weight="semibold">编辑用户: {editingUser.username}</Text>} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              <label>
                <Text size={200}>角色</Text>
                <select value={editRole} onChange={e => setEditRole(e.target.value)}
                  style={{ width: '100%', padding: '6px', borderRadius: '4px', border: '1px solid #ccc', marginTop: '4px' }}>
                  <option value="Admin">Admin</option>
                  <option value="Analyst">Analyst</option>
                  <option value="Auditor">Auditor</option>
                </select>
              </label>
              <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
                <Button onClick={() => setEditingUser(null)}>取消</Button>
                <Button appearance="primary" onClick={() => void updateUser()}>保存</Button>
              </div>
            </div>
          </Card>
        </div>
      )}
    </Card>
  );
}

function RolePermissions() {
  const [roles, setRoles] = useState<AdminRolePermissionResponse[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await apiClient.getAdminRoles();
        setRoles(response.roles ?? []);
      } catch (err) {
        setError(err instanceof Error ? err.message : '加载角色权限失败。');
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, []);

  if (loading) return <Spinner label="正在加载角色权限..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">角色权限</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>角色</TableHeaderCell>
            <TableHeaderCell>内置</TableHeaderCell>
            <TableHeaderCell>权限</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {roles.map((role) => (
            <TableRow key={role.role}>
              <TableCell><Text weight="semibold">{role.role}</Text></TableCell>
              <TableCell><Badge appearance="outline">{role.builtIn ? '是' : '否'}</Badge></TableCell>
              <TableCell>
                <div className="permission-badges">
                  {role.permissions.map((permission) => (
                    <Badge key={permission} appearance="outline">{permission}</Badge>
                  ))}
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  );
}

function AuditLog() {
  const [items, setItems] = useState<AuditRow[]>([]);
  const [summary, setSummary] = useState<AdminAuditSummaryResponse | null>(null);
  const [eventTypeFilter, setEventTypeFilter] = useState('');
  const [actorFilter, setActorFilter] = useState('');
  const [hostFilter, setHostFilter] = useState('');
  const [severityFilter, setSeverityFilter] = useState('');
  const [searchFilter, setSearchFilter] = useState('');
  const [pagination, setPagination] = useState<AdminAuditEventsResponse['pagination'] | null>(null);
  const [retentionDays, setRetentionDays] = useState('90');
  const [retentionResult, setRetentionResult] = useState<AdminAuditRetentionResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [retentionRunning, setRetentionRunning] = useState(false);

  const buildFilters = () => buildAdminAuditQuery({
    count: 100,
    eventType: eventTypeFilter,
    actorId: actorFilter,
    host: parseAuditNumberFilter<OfficeHost>(hostFilter),
    severity: parseAuditNumberFilter<AuditSeverity>(severityFilter),
    search: searchFilter,
  });

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const query = buildFilters();
      const [summaryResponse, eventsResponse] = await Promise.all([
        apiClient.getAdminAuditSummary(168, query),
        apiClient.getAdminAuditEvents(query),
      ]);
      setSummary(summaryResponse);
      setItems(eventsResponse.items ?? []);
      setPagination(eventsResponse.pagination ?? null);
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载审计日志失败。');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const exportCsv = async () => {
    recordUiAction({
      action: 'admin.audit.export_csv',
      metadata: buildFilters() as Record<string, string | number | boolean | undefined>,
    });
    setExporting(true);
    setError(null);
    try {
      await downloadBlob(
        await apiClient.downloadAdminAuditCsv({ ...buildFilters(), count: 500 }),
        `modelforge-audit-${new Date().toISOString().slice(0, 10)}.csv`,
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : '导出审计 CSV 失败。');
    } finally {
      setExporting(false);
    }
  };

  const applyRetention = async (dryRun: boolean) => {
    const days = parseAuditRetentionDays(retentionDays);
    if (days === undefined) {
      setError('审计保留天数必须在 1 到 3650 之间。');
      return;
    }

    recordUiAction({
      action: dryRun ? 'admin.audit.retention.preview' : 'admin.audit.retention.prune',
      metadata: { retentionDays: days },
    });
    setRetentionRunning(true);
    setError(null);
    try {
      const result = await apiClient.applyAdminAuditRetention({ retentionDays: days, dryRun });
      setRetentionResult(result);
      if (!dryRun) {
        await load();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '执行审计保留策略失败。');
    } finally {
      setRetentionRunning(false);
    }
  };

  const applyDrilldown = async (query: AdminAuditEventsQuery) => {
    const next = buildAdminAuditQuery({ ...buildFilters(), ...query });
    setEventTypeFilter(next.eventType ?? '');
    setActorFilter(next.actorId ?? '');
    setHostFilter(next.host === undefined ? '' : String(next.host));
    setSeverityFilter(next.severity === undefined ? '' : String(next.severity));
    setSearchFilter(next.search ?? '');
    recordUiAction({
      action: 'admin.audit.drilldown',
      metadata: next as Record<string, string | number | boolean | undefined>,
    });
    await loadWithQuery(next);
  };

  const loadWithQuery = async (query: AdminAuditEventsQuery) => {
    setLoading(true);
    setError(null);
    try {
      const normalized = buildAdminAuditQuery(query);
      const [summaryResponse, eventsResponse] = await Promise.all([
        apiClient.getAdminAuditSummary(168, normalized),
        apiClient.getAdminAuditEvents(normalized),
      ]);
      setSummary(summaryResponse);
      setItems(eventsResponse.items ?? []);
      setPagination(eventsResponse.pagination ?? null);
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载审计日志失败。');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <Spinner label="正在加载审计日志..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">审计日志</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      {summary && (
        <div className="audit-summary">
          <DiagnosticMetric label="窗口" value={`${summary.windowHours} 小时`} />
          <DiagnosticMetric label="事件数" value={summary.totalEvents} />
          <DiagnosticMetric label="趋势粒度" value={`${summary.bucketHours ?? 0} 小时`} />
          <AuditSummaryList
            title="事件类型 Top 10"
            items={summary.byEventType}
            onSelect={(item) => void applyDrilldown({ eventType: item.key })}
          />
          <AuditSummaryList
            title="用户 Top 10"
            items={summary.byActor}
            onSelect={(item) => void applyDrilldown({ actorId: item.key })}
          />
          <AuditSummaryList
            title="宿主"
            items={summary.byHost}
            onSelect={(item) => void applyDrilldown({ host: parseOfficeHostLabel(item.key) })}
          />
          <AuditTimeline title="事件趋势" items={summary.timeline ?? []} />
          <AuditHeatmap
            title="功能热力图"
            items={summary.heatmap ?? []}
            onSelect={(item) => void applyDrilldown({
              eventType: item.rowKey,
              host: parseOfficeHostLabel(item.columnKey),
            })}
          />
        </div>
      )}
      <div className="audit-filters">
        <Input placeholder="事件类型" value={eventTypeFilter} onChange={(_, data) => setEventTypeFilter(data.value)} />
        <Input placeholder="用户" value={actorFilter} onChange={(_, data) => setActorFilter(data.value)} />
        <Input placeholder="全文搜索" value={searchFilter} onChange={(_, data) => setSearchFilter(data.value)} />
        <select aria-label="宿主筛选" value={hostFilter} onChange={(event) => setHostFilter(event.target.value)}>
          <option value="">全部宿主</option>
          <option value={OfficeHost.Excel}>Excel</option>
          <option value={OfficeHost.PowerPoint}>PowerPoint</option>
          <option value={OfficeHost.Word}>Word</option>
          <option value={OfficeHost.Web}>Web</option>
        </select>
        <select aria-label="级别筛选" value={severityFilter} onChange={(event) => setSeverityFilter(event.target.value)}>
          <option value="">全部级别</option>
          <option value={AuditSeverity.Information}>Information</option>
          <option value={AuditSeverity.Warning}>Warning</option>
          <option value={AuditSeverity.Error}>Error</option>
          <option value={AuditSeverity.Critical}>Critical</option>
        </select>
        <Button onClick={() => void load()} disabled={loading}>应用筛选</Button>
        <Button onClick={exportCsv} disabled={exporting}>{exporting ? '导出中...' : '导出 CSV'}</Button>
      </div>
      <div className="audit-retention">
        <Text weight="semibold">审计保留策略</Text>
        <Input
          aria-label="审计保留天数"
          value={retentionDays}
          onChange={(_, data) => setRetentionDays(data.value)}
          placeholder="90"
        />
        <Button onClick={() => void applyRetention(true)} disabled={retentionRunning}>预览清理</Button>
        <Button appearance="secondary" onClick={() => void applyRetention(false)} disabled={retentionRunning}>执行清理</Button>
        {retentionResult && (
          <Text size={100}>
            保留 {retentionResult.retentionDays} 天，截止 {formatDate(retentionResult.cutoffUtc)}，
            匹配 {retentionResult.matchedEvents} 条，已删除 {retentionResult.deletedEvents} 条。
          </Text>
        )}
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>时间</TableHeaderCell>
            <TableHeaderCell>用户</TableHeaderCell>
            <TableHeaderCell>事件</TableHeaderCell>
            <TableHeaderCell>宿主 / 级别</TableHeaderCell>
            <TableHeaderCell>目标</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item, index) => (
            <TableRow key={`${item.eventId ?? index}`}>
              <TableCell><Text size={100}>{formatDate(item.recordedAtUtc)}</Text></TableCell>
              <TableCell>{item.actorId ?? '-'}</TableCell>
              <TableCell>{item.eventType ?? '-'}</TableCell>
              <TableCell><Text size={100}>{formatOfficeHost(item.host)} / {formatAuditSeverity(item.severity)}</Text></TableCell>
              <TableCell><Text size={100}>{item.commandId ?? item.resourceId ?? '-'}</Text></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Text size={100}>共 {pagination?.total ?? items.length} 条记录，当前显示 {items.length} 条</Text>
    </Card>
  );
}

function AuditSummaryList({
  title,
  items,
  onSelect,
}: {
  title: string;
  items: { key: string; count: number }[];
  onSelect?: (item: { key: string; count: number }) => void;
}) {
  return (
    <div className="audit-summary-list">
      <Text weight="semibold" size={100}>{title}</Text>
      {items.length === 0 ? (
        <Text size={100}>暂无数据</Text>
      ) : (
        items.map((item) => (
          <button
            key={item.key}
            type="button"
            className="audit-summary-row audit-drilldown-button"
            onClick={() => onSelect?.(item)}
            disabled={!onSelect}
            title={`按 ${item.key} 下钻`}
          >
            <Text size={100}>{item.key}</Text>
            <Badge appearance="outline">{item.count}</Badge>
          </button>
        ))
      )}
    </div>
  );
}

function AuditTimeline({ title, items }: { title: string; items: AdminAuditTimelineBucket[] }) {
  const maxCount = Math.max(1, ...items.map((item) => item.count));

  return (
    <div className="audit-summary-list audit-timeline">
      <Text weight="semibold" size={100}>{title}</Text>
      {items.length === 0 ? (
        <Text size={100}>暂无数据</Text>
      ) : (
        items.map((item) => (
          <div key={`${item.startUtc}-${item.endUtc}`} className="audit-timeline-row">
            <Text size={100}>{formatAuditTimelineLabel(item)}</Text>
            <div className="audit-timeline-bar">
              <span style={{ width: `${Math.max(4, (item.count / maxCount) * 100)}%` }} />
            </div>
            <Badge appearance="outline">{item.count}</Badge>
          </div>
        ))
      )}
    </div>
  );
}

function AuditHeatmap({
  title,
  items,
  onSelect,
}: {
  title: string;
  items: AdminAuditHeatmapCell[];
  onSelect?: (item: AdminAuditHeatmapCell) => void;
}) {
  const rows = Array.from(new Set(items.map((item) => item.rowKey))).sort((left, right) => left.localeCompare(right));
  const columns = Array.from(new Set(items.map((item) => item.columnKey))).sort((left, right) => left.localeCompare(right));
  const maxCount = Math.max(1, ...items.map((item) => item.count));
  const lookup = new Map(items.map((item) => [`${item.rowKey}\u0000${item.columnKey}`, item.count]));

  return (
    <div className="audit-summary-list audit-heatmap">
      <Text weight="semibold" size={100}>{title}</Text>
      {items.length === 0 ? (
        <Text size={100}>暂无数据</Text>
      ) : (
        <div className="audit-heatmap-grid" style={{ gridTemplateColumns: `minmax(120px, 1.5fr) repeat(${columns.length}, minmax(64px, 1fr))` }}>
          <Text size={100} weight="semibold">事件 / 宿主</Text>
          {columns.map((column) => <Text key={column} size={100} weight="semibold">{column}</Text>)}
          {rows.flatMap((row) => [
            <Text key={`${row}-label`} size={100}>{row}</Text>,
            ...columns.map((column) => {
              const count = lookup.get(`${row}\u0000${column}`) ?? 0;
              const item = { rowKey: row, columnKey: column, count };
              return (
                <button
                  key={`${row}-${column}`}
                  type="button"
                  className="audit-heatmap-cell audit-drilldown-button"
                  style={{ opacity: count === 0 ? 0.25 : Math.max(0.35, count / maxCount) }}
                  title={`${row} / ${column}: ${count}`}
                  onClick={() => onSelect?.(item)}
                  disabled={!onSelect || count === 0}
                >
                  {count}
                </button>
              );
            }),
          ])}
        </div>
      )}
    </div>
  );
}

function ConfigPanel() {
  const [values, setValues] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await apiClient.getConfig('global');
        setValues(response.values);
      } catch (err) {
        setError(err instanceof Error ? err.message : '加载配置失败。');
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, []);

  const toggle = async (key: string) => {
    const nextValues = { ...values, [key]: values[key] === 'true' ? 'false' : 'true' };
    setValues(nextValues);
    await apiClient.upsertConfig('global', { values: nextValues, updatedBy: 'web-admin' });
  };

  if (loading) return <Spinner label="正在加载配置..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">系统配置</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {Object.entries(values).map(([key, value]) => (
          <div key={key} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Text size={200}>{key}</Text>
            {value === 'true' || value === 'false' ? (
              <Button size="small" onClick={() => void toggle(key)}>
                <Badge appearance="outline">{value}</Badge>
              </Button>
            ) : (
              <Text size={100}>{value}</Text>
            )}
          </div>
        ))}
      </div>
    </Card>
  );
}

function DiagnosticsPanel() {
  const [diagnostics, setDiagnostics] = useState<AdminDiagnosticsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);

  const loadDiagnostics = async (audit = false) => {
    if (audit) {
      recordUiAction({ action: 'admin.diagnostics.refresh' });
    }
    setLoading(true);
    setError(null);
    try {
      setDiagnostics(await apiClient.getAdminDiagnostics());
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载系统诊断失败。');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadDiagnostics();
  }, []);

  const downloadBundle = async () => {
    recordUiAction({ action: 'admin.diagnostics.download' });
    setDownloading(true);
    setError(null);
    try {
      await downloadBlob(
        await apiClient.downloadAdminDiagnosticsBundle(),
        `modelforge-diagnostics-${new Date().toISOString().slice(0, 10)}.json`,
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : '下载诊断包失败。');
    } finally {
      setDownloading(false);
    }
  };

  if (loading) return <Spinner label="正在加载系统诊断..." />;

  return (
    <Card>
      <CardHeader
        header={<Text weight="semibold">系统诊断</Text>}
        action={(
          <div className="diagnostics-actions">
            <Button onClick={() => void loadDiagnostics(true)}>刷新</Button>
            <Button onClick={downloadBundle} disabled={downloading}>{downloading ? '下载中...' : '下载诊断包'}</Button>
          </div>
        )}
      />
      {error && <Text className="error-text">{error}</Text>}
      {diagnostics && (
        <div className="diagnostics-panel">
          <div className="diagnostics-grid">
            <DiagnosticMetric label="版本" value={diagnostics.version.version} />
            <DiagnosticMetric label="API" value={diagnostics.version.apiVersion} />
            <DiagnosticMetric label="数据库" value={`${diagnostics.databaseProvider} / ${diagnostics.databaseConnected ? '已连接' : '异常'}`} />
            <DiagnosticMetric label="命令" value={diagnostics.commandCount} />
            <DiagnosticMetric label="链接" value={diagnostics.linkCount} />
            <DiagnosticMetric label="词典" value={diagnostics.dictionaryTermCount} />
            <DiagnosticMetric label="审计" value={diagnostics.recentAuditEventCount} />
            <DiagnosticMetric label="审计保留" value={`${diagnostics.auditRetentionDays} 天`} />
            <DiagnosticMetric label="待清理审计" value={diagnostics.auditEventsEligibleForRetentionPrune} />
          </div>
          <div className="diagnostics-section">
            <Text weight="semibold">配置快照</Text>
            {Object.entries(diagnostics.configuration).length === 0 ? (
              <Text size={100}>暂无配置项</Text>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>键</TableHeaderCell>
                    <TableHeaderCell>值</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {Object.entries(diagnostics.configuration).map(([key, value]) => (
                    <TableRow key={key}>
                      <TableCell>{key}</TableCell>
                      <TableCell><Text size={100}>{value}</Text></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
          <div className="diagnostics-section">
            <Text weight="semibold">备注</Text>
            <div className="diagnostics-notes">
              {diagnostics.notes.map((note) => <Badge key={note} appearance="outline">{note}</Badge>)}
            </div>
          </div>
        </div>
      )}
    </Card>
  );
}

function DiagnosticMetric({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="diagnostic-metric">
      <Text size={100}>{label}</Text>
      <Text weight="semibold">{value}</Text>
    </div>
  );
}

function DictionaryPanel() {
  const [terms, setTerms] = useState<DictionaryTerm[]>([]);
  const [newTerm, setNewTerm] = useState('');
  const [sampleText, setSampleText] = useState('This DRAFT document contains TBD terms.');
  const [checkResult, setCheckResult] = useState<DictionaryCheckResponse | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const dictionaryMatches = checkResult?.matches ?? [];
  const highlightedSegments = useMemo(
    () => buildHighlightedTextSegments(sampleText, dictionaryMatches),
    [sampleText, dictionaryMatches],
  );

  const loadTerms = async () => {
    setLoading(true);
    setError(null);
    try {
      setTerms(await apiClient.getDictionaryTerms());
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载企业词典失败。');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadTerms();
  }, []);

  const addTerm = async () => {
    if (!newTerm.trim()) {
      return;
    }
    setError(null);
    setMessage(null);
    try {
      recordUiAction({ action: 'dictionary.term.add', metadata: { category: 'Custom' } });
      await apiClient.upsertDictionaryTerm({ term: newTerm.trim(), category: 'Custom', severity: 'Warning' });
      setNewTerm('');
      await loadTerms();
    } catch (err) {
      setError(err instanceof Error ? err.message : '保存术语失败。');
    }
  };

  const deleteTerm = async (id: string) => {
    setError(null);
    setMessage(null);
    try {
      recordUiAction({ action: 'dictionary.term.delete', resourceId: id });
      await apiClient.deleteDictionaryTerm(id);
      await loadTerms();
    } catch (err) {
      setError(err instanceof Error ? err.message : '删除术语失败。');
    }
  };

  const checkText = async () => {
    setError(null);
    setMessage(null);
    try {
      recordUiAction({ action: 'dictionary.check', metadata: { length: sampleText.length } });
      setCheckResult(await apiClient.checkDictionaryText({ text: sampleText, language: 'en' }));
    } catch (err) {
      setError(err instanceof Error ? err.message : '检查文本失败。');
    }
  };

  const exportJson = async () => {
    recordUiAction({ action: 'dictionary.export', metadata: { format: 'json' } });
    const data = await apiClient.exportDictionaryTerms();
    await downloadBlob(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }), 'modelforge-dictionary.json');
  };

  const exportCsv = async () => {
    recordUiAction({ action: 'dictionary.export', metadata: { format: 'csv' } });
    const data = await apiClient.exportDictionaryTerms();
    await downloadBlob(new Blob([formatDictionaryCsvPayload(data.terms)], { type: 'text/csv;charset=utf-8' }), 'modelforge-dictionary.csv');
  };

  const exportXlsx = async () => {
    recordUiAction({ action: 'dictionary.export', metadata: { format: 'xlsx' } });
    const data = await apiClient.exportDictionaryTerms();
    await downloadBlob(new Blob([await formatDictionaryXlsxPayload(data.terms)], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    }), 'modelforge-dictionary.xlsx');
  };

  const importTerms = async (file: File | undefined, parser: (payload: string) => DictionaryTerm[] | Promise<DictionaryTerm[]>) => {
    if (!file) {
      return;
    }
    setError(null);
    setMessage(null);
    try {
      const termsPayload = await parser(await file.text());
      recordUiAction({
        action: 'dictionary.import',
        metadata: { fileName: file.name, count: termsPayload.length },
      });
      const result = await apiClient.importDictionaryTerms({ terms: termsPayload, overwrite: true });
      setMessage(`已导入 ${result.imported} 条，跳过 ${result.skipped} 条。`);
      await loadTerms();
    } catch (err) {
      setError(err instanceof Error ? err.message : '导入企业词典失败。');
    }
  };

  const importTermsXlsx = async (file: File | undefined) => {
    if (!file) {
      return;
    }
    setError(null);
    setMessage(null);
    try {
      const termsPayload = await parseDictionaryXlsxPayload(await file.arrayBuffer());
      recordUiAction({
        action: 'dictionary.import',
        metadata: { format: 'xlsx', fileName: file.name, count: termsPayload.length },
      });
      const result = await apiClient.importDictionaryTerms({ terms: termsPayload, overwrite: true });
      setMessage(`已导入 ${result.imported} 条，跳过 ${result.skipped} 条。`);
      await loadTerms();
    } catch (err) {
      setError(err instanceof Error ? err.message : '导入 XLSX 企业词典失败。');
    }
  };

  if (loading) return <Spinner label="正在加载企业词典..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">企业词典</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      {message && <Text>{message}</Text>}
      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        <Input placeholder="新增术语" value={newTerm} onChange={(_, data) => setNewTerm(data.value)} />
        <Button appearance="primary" onClick={addTerm}>添加</Button>
      </div>
      <div className="dictionary-toolbar">
        <Button onClick={exportJson}>导出 JSON</Button>
        <Button onClick={exportCsv}>导出 CSV</Button>
        <Button onClick={exportXlsx}>导出 XLSX</Button>
        <label className="dictionary-import-label">
          <input
            aria-label="导入企业词典 JSON"
            type="file"
            accept="application/json,.json"
            onChange={(event) => {
              void importTerms(event.currentTarget.files?.[0], parseDictionaryImportPayload);
              event.currentTarget.value = '';
            }}
          />
          导入 JSON
        </label>
        <label className="dictionary-import-label">
          <input
            aria-label="导入企业词典 CSV"
            type="file"
            accept="text/csv,.csv"
            onChange={(event) => {
              void importTerms(event.currentTarget.files?.[0], parseDictionaryCsvPayload);
              event.currentTarget.value = '';
            }}
          />
          导入 CSV
        </label>
        <label className="dictionary-import-label">
          <input
            aria-label="导入企业词典 XLSX"
            type="file"
            accept="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,.xlsx"
            onChange={(event) => {
              void importTermsXlsx(event.currentTarget.files?.[0]);
              event.currentTarget.value = '';
            }}
          />
          导入 XLSX
        </label>
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>术语</TableHeaderCell>
            <TableHeaderCell>分类</TableHeaderCell>
            <TableHeaderCell>级别</TableHeaderCell>
            <TableHeaderCell>替换建议</TableHeaderCell>
            <TableHeaderCell>操作</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {terms.map((term) => (
            <TableRow key={term.id}>
              <TableCell><Text weight="semibold">{term.term}</Text></TableCell>
              <TableCell>{term.category}</TableCell>
              <TableCell><Badge appearance="outline">{term.severity}</Badge></TableCell>
              <TableCell>{term.replacement ?? '-'}</TableCell>
              <TableCell><Button size="small" onClick={() => void deleteTerm(term.id)}>删除</Button></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <div className="dictionary-check">
        <Textarea value={sampleText} onChange={(_, data) => setSampleText(data.value)} resize="vertical" />
        <Button onClick={checkText}>检查文本</Button>
        {checkResult && (
          <div className="dictionary-check-result">
            <Text size={200} weight="semibold">
              命中 {checkResult.matchCount} 条
              {checkResult.cleanedText ? `，建议文本：${checkResult.cleanedText}` : ''}
            </Text>
            <div className="dictionary-highlighted-text" aria-label="词典检查高亮结果">
              {highlightedSegments.map((segment, index) => (
                segment.match ? (
                  <mark
                    key={`${segment.match.termId}-${segment.match.position}-${index}`}
                    className="dictionary-highlight"
                    title={segment.match.suggestion ?? segment.match.term}
                  >
                    {segment.text}
                  </mark>
                ) : (
                  <span key={`plain-${index}`}>{segment.text}</span>
                )
              ))}
            </div>
            {dictionaryMatches.length > 0 && (
              <div className="dictionary-match-list">
                {dictionaryMatches.map((match, index) => (
                  <div key={`${match.termId}-${match.position}-${index}`} className="dictionary-match-row">
                    <Badge appearance="outline">{match.term}</Badge>
                    <Text size={100}>
                      命中「{match.matchedText}」@ {match.position}
                      {match.suggestion ? `，建议替换为「${match.suggestion}」` : ''}
                    </Text>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </Card>
  );
}

async function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

function formatDate(value?: string) {
  if (!value) return '-';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function formatAuditTimelineLabel(item: AdminAuditTimelineBucket) {
  const start = new Date(item.startUtc);
  const end = new Date(item.endUtc);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
    return item.startUtc;
  }

  const day = start.toISOString().slice(0, 10);
  return `${day} ${start.getUTCHours().toString().padStart(2, '0')}:00-${end.getUTCHours().toString().padStart(2, '0')}:00`;
}

function buildAdminAuditQuery(query: AdminAuditEventsQuery): AdminAuditEventsQuery {
  const normalized: AdminAuditEventsQuery = {};
  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null) {
      return;
    }

    if (typeof value === 'string') {
      const trimmed = value.trim();
      if (trimmed) {
        normalized[key as keyof AdminAuditEventsQuery] = trimmed as never;
      }
      return;
    }

    normalized[key as keyof AdminAuditEventsQuery] = value as never;
  });

  return normalized;
}

function parseAuditRetentionDays(value: string): number | undefined {
  const days = Number(value);
  return Number.isInteger(days) && days >= 1 && days <= 3650 ? days : undefined;
}

function parseAuditNumberFilter<T extends number>(value: string): T | undefined {
  if (!value) {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed as T : undefined;
}

function formatOfficeHost(host: OfficeHost) {
  return OfficeHost[host] ?? String(host);
}

function parseOfficeHostLabel(value: string): OfficeHost | undefined {
  const normalized = value.trim();
  if (!normalized) {
    return undefined;
  }

  const numeric = Number(normalized);
  if (Number.isFinite(numeric) && OfficeHost[numeric as OfficeHost] !== undefined) {
    return numeric as OfficeHost;
  }

  const match = Object.entries(OfficeHost)
    .find(([key, entryValue]) => Number.isNaN(Number(key)) &&
      key.toLowerCase() === normalized.toLowerCase() &&
      typeof entryValue === 'number');
  return match ? match[1] as OfficeHost : undefined;
}

function formatAuditSeverity(severity: AuditSeverity) {
  return AuditSeverity[severity] ?? String(severity);
}

export const __adminConsoleTestables = {
  buildHighlightedTextSegments,
  formatDictionaryCsvPayload,
  formatDictionaryXlsxPayload,
  parseDictionaryImportPayload,
  parseDictionaryCsvPayload,
  parseDictionaryXlsxPayload,
  formatAuditTimelineLabel,
  buildAdminAuditQuery,
  parseAuditRetentionDays,
  parseAuditNumberFilter,
  formatOfficeHost,
  parseOfficeHostLabel,
  formatAuditSeverity,
  unwrapJson,
};
