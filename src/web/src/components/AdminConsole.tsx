import { useState } from 'react';
import {
  Button, Card, CardHeader, Input, Text, Title3, Badge,
  Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell,
} from '@fluentui/react-components';

/**
 * Admin Console — 用户管理与审计日志查看器。
 * 当前使用本地 mock 数据；生产环境通过后端 API 拉取。
 */
export function AdminConsole() {
  const [activeTab, setActiveTab] = useState<'users' | 'audit' | 'config'>('users');

  return (
    <div className="panel">
      <Title3>管理控制台</Title3>

      <div className="admin-tabs">
        {(['users', 'audit', 'config'] as const).map(tab => (
          <button
            key={tab}
            className={`admin-tab${activeTab === tab ? ' active' : ''}`}
            onClick={() => setActiveTab(tab)}
          >
            {tab === 'users' ? '用户管理' : tab === 'audit' ? '审计日志' : '配置'}
          </button>
        ))}
      </div>

      {activeTab === 'users' && <UserManagement />}
      {activeTab === 'audit' && <AuditLog />}
      {activeTab === 'config' && <ConfigPanel />}
    </div>
  );
}

// ─── User Management ──────────────────────────────────────────────

function UserManagement() {
  const [users, setUsers] = useState([
    { id: '1', name: 'admin', role: '管理员', status: '活跃', lastSeen: '2026-06-03 09:15' },
    { id: '2', name: 'analyst01', role: '分析师', status: '活跃', lastSeen: '2026-06-03 08:30' },
    { id: '3', name: 'auditor03', role: '审计员', status: '已禁用', lastSeen: '2026-05-28 14:00' },
  ]);
  const [newName, setNewName] = useState('');

  const addUser = () => {
    if (!newName.trim()) return;
    setUsers(prev => [...prev, {
      id: crypto.randomUUID(),
      name: newName.trim(),
      role: '分析师',
      status: '活跃',
      lastSeen: '-',
    }]);
    setNewName('');
  };

  const toggleStatus = (id: string) => {
    setUsers(prev => prev.map(u =>
      u.id === id ? { ...u, status: u.status === '活跃' ? '已禁用' : '活跃' } : u
    ));
  };

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">用户列表</Text>} />
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
            <TableHeaderCell>最近活跃</TableHeaderCell>
            <TableHeaderCell>操作</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map(u => (
            <TableRow key={u.id}>
              <TableCell><Text weight="semibold">{u.name}</Text></TableCell>
              <TableCell>{u.role}</TableCell>
              <TableCell>
                <Badge color={u.status === '活跃' ? 'success' : 'warning'}>{u.status}</Badge>
              </TableCell>
              <TableCell><Text size={100}>{u.lastSeen}</Text></TableCell>
              <TableCell>
                <Button size="small" onClick={() => toggleStatus(u.id)}>
                  {u.status === '活跃' ? '禁用' : '启用'}
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  );
}

// ─── Audit Log ────────────────────────────────────────────────────

function AuditLog() {
  const [logs] = useState([
    { id: '1', user: 'admin', action: 'command.dispatch', target: 'excel.fill-right', time: '2026-06-03 09:15:30' },
    { id: '2', user: 'analyst01', action: 'config.update', target: 'BackendBridgeMode', time: '2026-06-03 08:30:12' },
    { id: '3', user: 'analyst01', action: 'model.check', target: 'Sheet1!A1:D20', time: '2026-06-03 08:25:01' },
    { id: '4', user: 'admin', action: 'command.dispatch', target: 'excel.prepare-to-share', time: '2026-06-02 17:40:00' },
    { id: '5', user: 'auditor03', action: 'link.refresh', target: 'excel→ppt', time: '2026-06-02 14:10:22' },
    { id: '6', user: 'admin', action: 'user.disable', target: 'auditor03', time: '2026-05-28 14:00:00' },
  ]);
  const [filter, setFilter] = useState('');

  const filtered = logs.filter(l =>
    !filter || l.user.includes(filter) || l.action.includes(filter) || l.target.includes(filter)
  );

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">审计日志</Text>} />
      <Input
        placeholder="筛选 (用户/操作/目标)..."
        value={filter}
        onChange={(_, d) => setFilter(d.value)}
        style={{ marginBottom: 12 }}
      />
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>时间</TableHeaderCell>
            <TableHeaderCell>用户</TableHeaderCell>
            <TableHeaderCell>操作</TableHeaderCell>
            <TableHeaderCell>目标</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {filtered.map(l => (
            <TableRow key={l.id}>
              <TableCell><Text size={100}>{l.time}</Text></TableCell>
              <TableCell><Text weight="semibold">{l.user}</Text></TableCell>
              <TableCell>{l.action}</TableCell>
              <TableCell><Text size={100}>{l.target}</Text></TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Text size={100} style={{ marginTop: 8 }}>共 {filtered.length} 条记录 (Mock 数据)</Text>
    </Card>
  );
}

// ─── Config Panel ─────────────────────────────────────────────────

function ConfigPanel() {
  const [config, setConfig] = useState({
    TelemetryEnabled: 'false',
    DefaultLanguage: 'zh-CN',
    BackendBridgeMode: 'local-development',
    KeyboardHookEnabled: 'true',
    SidecarPort: '5200',
  });

  const toggle = (key: string) => {
    setConfig(prev => ({
      ...prev,
      [key]: prev[key as keyof typeof prev] === 'true' ? 'false' : 'true',
    }));
  };

  return (
    <Card>
      <CardHeader header={<Text weight="semibold">系统配置</Text>} />
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {Object.entries(config).map(([key, val]) => (
          <div key={key} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Text size={200}>{key}</Text>
            {val === 'true' || val === 'false' ? (
              <Button size="small" onClick={() => toggle(key)}>
                <Badge color={val === 'true' ? 'success' : 'warning'}>{val}</Badge>
              </Button>
            ) : (
              <Text size={100}>{val}</Text>
            )}
          </div>
        ))}
      </div>
      <Text size={100} style={{ marginTop: 12 }}>⚠️ 当前为 Mock 配置（不持久化）</Text>
    </Card>
  );
}
