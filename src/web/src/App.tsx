import { useEffect, useState } from 'react';
import {
  Button, Card, CardHeader, Spinner, Text, Title3, Badge,
} from '@fluentui/react-components';
import { useBridgeStore, type PanelId } from './services/bridgeStore';
import { useAuthStore } from './services/authStore';
import { LoginPage } from './components/LoginPage';
import { Omnibar } from './components/Omnibar';
import { AiwaChat } from './components/AiwaChat';
import { AdminConsole } from './components/AdminConsole';
// Icon components using simple SVG
const Icon = ({ d }: { d: string }) => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d={d} /></svg>
);
const HomeIcon = <Icon d="M12 3L4 9v12h6v-7h4v7h6V9z" />;
const AppsIcon = <Icon d="M4 8h4V4H4v4zm6 12h4v-4h-4v4zm-6 0h4v-4H4v4zm0-6h4v-4H4v4zm6 0h4v-4h-4v4zm6-10v4h4V4h-4zm-6 4h4V4h-4v4zm6 6h4v-4h-4v4zm0 6h4v-4h-4v4z" />;
const ExcelIcon = <Icon d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6zM6 20V4h7v5h5v11H6z" />;
const AuditIcon = <Icon d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />;
const AiIcon = <Icon d="M21 8V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2h5l3-6 3 6h5a2 2 0 002-2v-2M8 14a2 2 0 100-4 2 2 0 000 4zm8 0a2 2 0 100-4 2 2 0 000 4z" />;
const AdminIcon = <Icon d="M12 12c2.2 0 4-1.8 4-4s-1.8-4-4-4-4 1.8-4 4 1.8 4 4 4zm0 2c-2.7 0-8 1.3-8 4v2h16v-2c0-2.7-5.3-4-8-4z" />;

export function App() {
  const {
    backendBaseUrl, health, version, commands, isLoading, error,
    sidecarHealth, sidecarConnected, excelInfo, activePanel,
    refresh, checkSidecar, executeCommand, setActivePanel,
  } = useBridgeStore();

  const { isLoggedIn, user, login, logout } = useAuthStore();

  useEffect(() => { if (isLoggedIn) { void refresh(); void checkSidecar(); } }, [isLoggedIn]);

  const [showOmnibar, setShowOmnibar] = useState(false);

  // 未登录 → 显示登录页
  if (!isLoggedIn) {
    return <LoginPage onLogin={(token, u) => login(token, u)} />;
  }

  const navItems: { id: PanelId; label: string; icon: JSX.Element }[] = [
    { id: 'dashboard', label: '总览', icon: HomeIcon },
    { id: 'commands', label: '命令', icon: AppsIcon },
    { id: 'sidecar', label: 'Excel', icon: ExcelIcon },
    { id: 'audit', label: '审计', icon: AuditIcon },
    { id: 'aiwa', label: 'AIWA', icon: AiIcon },
    { id: 'admin', label: '管理', icon: AdminIcon },
  ];

  return (
    <main className="app-shell">
      {/* ── Side Navigation ── */}
      <nav className="side-nav">
        <div className="nav-brand">ModelForge</div>
        {navItems.map(item => (
          <button
            key={item.id}
            className={`nav-item${activePanel === item.id ? ' active' : ''}`}
            onClick={() => setActivePanel(item.id)}
          >
            <span className="nav-icon">{item.icon}</span>
            <span className="nav-label">{item.label}</span>
          </button>
        ))}
        <div className="nav-footer">
          {user && <Text size={100} style={{color:'#888',textAlign:'center'}}>{user.username}</Text>}
          <button className="nav-item" onClick={() => { void refresh(); void checkSidecar(); }} title="刷新">
            <span className="nav-icon"><Icon d="M17.65 6.35A7.96 7.96 0 0012 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08A5.99 5.99 0 0112 18c-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z" /></span>
          </button>
          <button className="nav-item" onClick={logout} title="退出登录">
            <span className="nav-icon"><Icon d="M16 13v-2H7V8l-5 4 5 4v-3z" /></span>
          </button>
        </div>
      </nav>

      {/* ── Content Panel ── */}
      <section className="content">
        {isLoading && <Spinner label="正在连接..." />}
        {error && <Text className="error-banner">{error}</Text>}

        {activePanel === 'dashboard' && (
          <Dashboard
            health={health}
            version={version}
            sidecarConnected={sidecarConnected}
            commands={commands}
            backendBaseUrl={backendBaseUrl}
          />
        )}

        {activePanel === 'commands' && (
          <CommandPanel
            commands={commands}
            onExecute={(id) => { void executeCommand(id); }}
          />
        )}

        {activePanel === 'sidecar' && (
          <SidecarPanel
            sidecarHealth={sidecarHealth}
            sidecarConnected={sidecarConnected}
            excelInfo={excelInfo}
          />
        )}

        {activePanel === 'audit' && <AuditPanel commands={commands} />}

        {activePanel === 'aiwa' && <AiwaChat />}

        {activePanel === 'admin' && <AdminConsole />}
      </section>

      {/* ── Omnibar Overlay ── */}
      {showOmnibar && <Omnibar onClose={() => setShowOmnibar(false)} />}

      {/* ── Omnibar Trigger (Command Palette) ── */}
      <button className="omnibar-trigger" onClick={() => setShowOmnibar(true)} title="搜索命令 (Ctrl+Shift+P)">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
          <path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0016 9.5 6.5 6.5 0 109.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z" />
        </svg>
      </button>
    </main>
  );
}

// ─── Dashboard Panel ──────────────────────────────────────────────

function Dashboard({ health, version, sidecarConnected, commands, backendBaseUrl }: {
  health?: { status: string };
  version?: { version: string };
  sidecarConnected: boolean;
  commands: { id: string }[];
  backendBaseUrl: string;
}) {
  return (
    <div className="panel">
      <Title3>总览</Title3>
      <div className="card-grid">
        <Card>
          <CardHeader header={<Text weight="semibold">后端 API</Text>} />
          <StatItem label="状态" value={health?.status ?? '未连接'} ok={!!health} />
          <StatItem label="版本" value={version?.version ?? '-'} />
          <Text size={100}>{backendBaseUrl}</Text>
        </Card>
        <Card>
          <CardHeader header={<Text weight="semibold">Sidecar</Text>} />
          <StatItem label="状态" value={sidecarConnected ? '已连接' : '未连接'} ok={sidecarConnected} />
          <Text size={100}>localhost:5200</Text>
        </Card>
        <Card>
          <CardHeader header={<Text weight="semibold">命令目录</Text>} />
          <Text size={900}>{commands.length}</Text>
          <Text size={100}>条已注册命令</Text>
        </Card>
      </div>
    </div>
  );
}

function StatItem({ label, value, ok }: { label: string; value: string; ok?: boolean }) {
  return (
    <div className="stat-item">
      <Text size={200}>{label}</Text>
      <Badge appearance={ok ? 'filled' : 'ghost'} color={ok ? 'success' : 'warning'}>
        {value}
      </Badge>
    </div>
  );
}

// ─── Command Panel ────────────────────────────────────────────────

const COMMAND_GROUPS: Record<string, string[]> = {
  'Power Tools': ['fill-right', 'fill-down', 'wrap-iferror', 'insert-statistics'],
  '财务格式': ['apply-finance-format', 'toggle-sign', 'insert-dcf-template'],
  '视觉审计': ['visualize-inputs', 'visualize-formulas', 'visualize-links', 'clear-visualizations'],
  '模型检查': ['model-check'],
  '公式追踪': ['trace-precedents', 'trace-dependents', 'clear-trace'],
  '工作簿': ['optimize-workbook', 'prepare-to-share'],
  '跨应用': ['link-to-powerpoint', 'refresh-links'],
};

function CommandPanel({ commands, onExecute }: {
  commands: { id: string; displayName: string; category: string; defaultShortcut?: string }[];
  onExecute: (id: string) => void;
}) {
  const cmdMap = new Map(commands.map(c => [c.id, c]));

  return (
    <div className="panel">
      <Title3>命令</Title3>
      {Object.entries(COMMAND_GROUPS).map(([group, ids]) => (
        <div key={group} className="command-group">
          <Text weight="semibold">{group}</Text>
          <div className="cmd-grid">
            {ids.map(id => {
              const cmd = cmdMap.get(id);
              if (!cmd) return null;
              return (
                <Button
                  key={id}
                  size="small"
                  onClick={() => onExecute(id)}
                  title={cmd.defaultShortcut ?? ''}
                >
                  {cmd.displayName}
                </Button>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

// ─── Sidecar Panel ────────────────────────────────────────────────

function SidecarPanel({ sidecarHealth, sidecarConnected, excelInfo }: {
  sidecarHealth?: { service: string; timestampUtc: string };
  sidecarConnected: boolean;
  excelInfo?: { connected: boolean; workbook?: string; worksheet?: string; selection?: string };
}) {
  return (
    <div className="panel">
      <Title3>Excel 连接</Title3>
      <Card>
        <CardHeader header={<Text weight="semibold">Sidecar 服务</Text>} />
        <StatItem label="状态" value={sidecarConnected ? '运行中' : '未连接'} ok={sidecarConnected} />
        {sidecarHealth && <Text size={100}>服务: {sidecarHealth.service}</Text>}
      </Card>
      {excelInfo?.connected && (
        <Card>
          <CardHeader header={<Text weight="semibold">活动工作簿</Text>} />
          <StatItem label="工作簿" value={excelInfo.workbook ?? '-'} />
          <StatItem label="工作表" value={excelInfo.worksheet ?? '-'} />
          <StatItem label="选中区域" value={excelInfo.selection ?? '-'} />
        </Card>
      )}
    </div>
  );
}

// ─── Audit Panel ──────────────────────────────────────────────────

function AuditPanel({ commands }: { commands: { id: string; displayName: string }[] }) {
  const auditCmds = commands.filter(c =>
    c.id.startsWith('excel.visualize') || c.id === 'excel.model-check' || c.id === 'excel.clear-visualizations'
  );

  return (
    <div className="panel">
      <Title3>模型审计</Title3>
      <Card>
        <CardHeader header={<Text weight="semibold">审计命令</Text>} />
        <ul>
          {auditCmds.map(c => (
            <li key={c.id}>
              <Text>{c.displayName}</Text> <Text size={100}>({c.id})</Text>
            </li>
          ))}
        </ul>
      </Card>
    </div>
  );
}
