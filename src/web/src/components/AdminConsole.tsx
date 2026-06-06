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
import type { DictionaryCheckResponse, DictionaryTerm } from '../types/contracts';

type AdminTab = 'users' | 'audit' | 'config' | 'dictionary';

export function AdminConsole() {
  const [activeTab, setActiveTab] = useState<AdminTab>('users');

  return (
    <div className="panel">
      <Title3>管理控制台</Title3>

      <div className="admin-tabs">
        {(['users', 'audit', 'config', 'dictionary'] as const).map(tab => (
          <button
            key={tab}
            className={`admin-tab${activeTab === tab ? ' active' : ''}`}
            onClick={() => setActiveTab(tab)}
          >
            {tab === 'users' ? '用户管理' : tab === 'audit' ? '审计日志' : tab === 'config' ? '配置' : '企业词典'}
          </button>
        ))}
      </div>

      {activeTab === 'users' && <UserManagement />}
      {activeTab === 'audit' && <AuditLog />}
      {activeTab === 'config' && <ConfigPanel />}
      {activeTab === 'dictionary' && <DictionaryPanel />}
    </div>
  );
}

function UserManagement() {
  const [users, setUsers] = useState<Array<{ id: string; username: string; role: string; isActive: boolean; createdAt?: string }>>([]);
  const [newName, setNewName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const loadUsers = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(`${apiClient.getBackendBaseUrl()}/api/admin/users`, {
        headers: { Authorization: `Bearer ${getStoredToken()}` },
      });
      if (!response.ok) throw new Error(`Load users failed: ${response.status}`);
      setUsers(await response.json());
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  const addUser = async () => {
    if (!newName.trim()) return;
    setError(null);
    try {
      const response = await fetch(`${apiClient.getBackendBaseUrl()}/api/admin/users`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${getStoredToken()}`,
        },
        body: JSON.stringify({ username: newName.trim(), password: 'ChangeMe123!' }),
      });
      if (!response.ok) throw new Error(`Create user failed: ${response.status}`);
      setNewName('');
      await loadUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create user');
    }
  };

  const toggleStatus = async (id: string) => {
    setError(null);
    try {
      const response = await fetch(`${apiClient.getBackendBaseUrl()}/api/admin/users/${id}/toggle`, {
        method: 'PUT',
        headers: { Authorization: `Bearer ${getStoredToken()}` },
      });
      if (!response.ok) throw new Error(`Toggle user failed: ${response.status}`);
      await loadUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to toggle user');
    }
  };

  if (loading) return <Spinner label="Loading users..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">用户列表</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        <Input placeholder="用户名" value={newName} onChange={(_, d) => setNewName(d.value)} />
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
          {users.map(user => (
            <TableRow key={user.id}>
              <TableCell><Text weight="semibold">{user.username}</Text></TableCell>
              <TableCell>{user.role}</TableCell>
              <TableCell>
                <Badge color={user.isActive ? 'success' : 'warning'}>{user.isActive ? '活跃' : '已禁用'}</Badge>
              </TableCell>
              <TableCell><Text size={100}>{formatDate(user.createdAt)}</Text></TableCell>
              <TableCell>
                <Button size="small" onClick={() => toggleStatus(user.id)}>
                  {user.isActive ? '禁用' : '启用'}
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  );
}

function AuditLog() {
  const [items, setItems] = useState<Array<Record<string, string>>>([]);
  const [filter, setFilter] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const response = await fetch(`${apiClient.getBackendBaseUrl()}/api/admin/audit-events?count=100`, {
          headers: { Authorization: `Bearer ${getStoredToken()}` },
        });
        if (!response.ok) throw new Error(`Load audit failed: ${response.status}`);
        const envelope = await response.json();
        setItems(envelope.data?.items ?? []);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load audit events');
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, []);

  const filtered = useMemo(
    () => items.filter(item => !filter || JSON.stringify(item).toLowerCase().includes(filter.toLowerCase())),
    [filter, items],
  );

  if (loading) return <Spinner label="Loading audit events..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">审计日志</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <Input placeholder="筛选 (用户/操作/目标)..." value={filter} onChange={(_, d) => setFilter(d.value)} />
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>时间</TableHeaderCell>
            <TableHeaderCell>用户</TableHeaderCell>
            <TableHeaderCell>事件</TableHeaderCell>
            <TableHeaderCell>目标</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {filtered.map((item, index) => (
            <TableRow key={`${item.eventId ?? index}`}>
              <TableCell><Text size={100}>{formatDate(item.recordedAtUtc)}</Text></TableCell>
              <TableCell>{item.actorId ?? '-'}</TableCell>
              <TableCell>{item.eventType ?? '-'}</TableCell>
              <TableCell><Text size={100}>{item.commandId ?? item.resourceId ?? '-'}</Text></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Text size={100}>共 {filtered.length} 条记录</Text>
    </Card>
  );
}

function ConfigPanel() {
  const [values, setValues] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const response = await apiClient.getConfig('global');
        setValues(response.values);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load config');
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, []);

  const toggle = async (key: string) => {
    const nextValues = {
      ...values,
      [key]: values[key] === 'true' ? 'false' : 'true',
    };
    setValues(nextValues);
    await apiClient.upsertConfig('global', { values: nextValues, updatedBy: 'web-admin' });
  };

  if (loading) return <Spinner label="Loading config..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">系统配置</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {Object.entries(values).map(([key, value]) => (
          <div key={key} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Text size={200}>{key}</Text>
            {value === 'true' || value === 'false' ? (
              <Button size="small" onClick={() => toggle(key)}>
                <Badge color={value === 'true' ? 'success' : 'warning'}>{value}</Badge>
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

function DictionaryPanel() {
  const [terms, setTerms] = useState<DictionaryTerm[]>([]);
  const [draft, setDraft] = useState({ term: '', replacement: '', regexPattern: '', category: 'Compliance', severity: 'Warning' });
  const [sampleText, setSampleText] = useState('This draft is confidential and EBITDA is TBD.');
  const [checkResult, setCheckResult] = useState<DictionaryCheckResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const loadTerms = async () => {
    setLoading(true);
    setError(null);
    try {
      setTerms(await apiClient.getDictionaryTerms());
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load dictionary');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadTerms();
  }, []);

  const addTerm = async () => {
    if (!draft.term.trim()) return;
    await apiClient.upsertDictionaryTerm({
      term: draft.term.trim(),
      replacement: draft.replacement.trim() || undefined,
      regexPattern: draft.regexPattern.trim() || undefined,
      category: draft.category.trim() || 'General',
      severity: draft.severity,
    });
    setDraft({ term: '', replacement: '', regexPattern: '', category: 'Compliance', severity: 'Warning' });
    await loadTerms();
  };

  const deleteTerm = async (id: string) => {
    await apiClient.deleteDictionaryTerm(id);
    await loadTerms();
  };

  const checkText = async () => {
    setCheckResult(await apiClient.checkDictionaryText({ text: sampleText, language: 'zh-CN' }));
  };

  if (loading) return <Spinner label="Loading dictionary..." />;

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">Corporate Dictionary</Text>} />
      {error && <Text className="error-text">{error}</Text>}
      <div className="dictionary-form">
        <Input placeholder="术语" value={draft.term} onChange={(_, d) => setDraft(prev => ({ ...prev, term: d.value }))} />
        <Input placeholder="替换建议" value={draft.replacement} onChange={(_, d) => setDraft(prev => ({ ...prev, replacement: d.value }))} />
        <Input placeholder="正则表达式（可选）" value={draft.regexPattern} onChange={(_, d) => setDraft(prev => ({ ...prev, regexPattern: d.value }))} />
        <Input placeholder="分类" value={draft.category} onChange={(_, d) => setDraft(prev => ({ ...prev, category: d.value }))} />
        <Input placeholder="级别" value={draft.severity} onChange={(_, d) => setDraft(prev => ({ ...prev, severity: d.value }))} />
        <Button appearance="primary" onClick={addTerm}>添加/更新</Button>
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
          {terms.map(term => (
            <TableRow key={term.id}>
              <TableCell><Text weight="semibold">{term.term}</Text></TableCell>
              <TableCell>{term.category}</TableCell>
              <TableCell><Badge color={term.severity === 'Error' ? 'danger' : 'warning'}>{term.severity}</Badge></TableCell>
              <TableCell>{term.replacement ?? '-'}</TableCell>
              <TableCell><Button size="small" onClick={() => deleteTerm(term.id)}>删除</Button></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <div className="dictionary-check">
        <Textarea value={sampleText} onChange={(_, d) => setSampleText(d.value)} resize="vertical" />
        <Button onClick={checkText}>检查文本</Button>
        {checkResult && (
          <Text size={200}>
            命中 {checkResult.matchCount} 项
            {checkResult.cleanedText ? `，建议文本：${checkResult.cleanedText}` : ''}
          </Text>
        )}
      </div>
    </Card>
  );
}

function formatDate(value?: string) {
  if (!value) return '-';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function getStoredToken() {
  return localStorage.getItem('modelforge_token') ?? '';
}
