import { useEffect, useState } from 'react';
import {
  Badge, Button, Card, CardHeader, Spinner, Text, Title3,
} from '@fluentui/react-components';
import { useBridgeStore, type PanelId } from './services/bridgeStore';
import { useAuthStore } from './services/authStore';
import { LoginPage } from './components/LoginPage';
import { Omnibar } from './components/Omnibar';
import { AiwaChat } from './components/AiwaChat';
import { AdminConsole } from './components/AdminConsole';
import { LinkManager } from './components/LinkManager';
import { DeckCheckViewer } from './components/DeckCheckViewer';
import { sidecarClient } from './services/sidecarClient';
import { recordUiAction } from './services/uiAudit';
import type { ShortcutItem } from './types/contracts';

type CommandSummary = {
  id: string;
  displayName: string;
  category: string;
  defaultShortcut?: string;
};

function parseShortcutImportPayload(payloadText: string): ShortcutItem[] {
  const payload = JSON.parse(payloadText) as { shortcuts?: ShortcutItem[] } | ShortcutItem[];
  const shortcuts = Array.isArray(payload) ? payload : payload.shortcuts;
  if (!Array.isArray(shortcuts)) {
    throw new Error('导入文件必须是快捷键数组，或包含 shortcuts 数组的 JSON 对象。');
  }

  return shortcuts;
}

function getStoredSidecarToken(): string {
  return localStorage.getItem('modelforge_sidecar_token') ?? '';
}

function saveStoredSidecarToken(token: string) {
  const normalized = token.trim();
  if (normalized) {
    localStorage.setItem('modelforge_sidecar_token', normalized);
  } else {
    localStorage.removeItem('modelforge_sidecar_token');
  }
}

const Icon = ({ d }: { d: string }) => (
  <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d={d} /></svg>
);

const HomeIcon = <Icon d="M12 3L4 9v12h6v-7h4v7h6V9z" />;
const AppsIcon = <Icon d="M4 8h4V4H4v4zm6 12h4v-4h-4v4zm-6 0h4v-4H4v4zm0-6h4v-4H4v4zm6 0h4v-4h-4v4zm6-10v4h4V4h-4zm-6 4h4V4h-4v4zm6 6h4v-4h-4v4zm0 6h4v-4h-4v4z" />;
const PptIcon = <Icon d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6zM6 20V4h7v5h5v11H6zM8 12h8v2H8v-2zM8 16h8v2H8v-2z" />;
const WordIcon = <Icon d="M4 4h10l6 6v10a2 2 0 01-2 2H4a2 2 0 01-2-2V6a2 2 0 012-2zm9 1.5V10h4.5L13 5.5zM7 13h10v2H7v-2zm0 4h10v2H7v-2z" />;
const ExcelIcon = <Icon d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8l-6-6zM6 20V4h7v5h5v11H6z" />;
const AuditIcon = <Icon d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />;
const AiIcon = <Icon d="M21 8V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2h5l3-6 3 6h5a2 2 0 002-2v-2M8 14a2 2 0 100-4 2 2 0 000 4zm8 0a2 2 0 100-4 2 2 0 000 4z" />;
const AdminIcon = <Icon d="M12 12c2.2 0 4-1.8 4-4s-1.8-4-4-4-4 1.8-4 4 1.8 4 4 4zm0 2c-2.7 0-8 1.3-8 4v2h16v-2c0-2.7-5.3-4-8-4z" />;
const LinkIcon = <Icon d="M3.9 12a5 5 0 015-5h4v2h-4a3 3 0 000 6h4v2h-4a5 5 0 01-5-5zm6.1 1v-2h4v2h-4zm1-6h4a5 5 0 010 10h-4v-2h4a3 3 0 000-6h-4V7z" />;

export const COMMAND_GROUPS: Record<string, string[]> = {
  'Power Tools': ['excel.fill-right', 'excel.fill-down', 'excel.wrap-iferror', 'excel.insert-statistics'],
  财务格式: ['excel.apply-finance-format', 'excel.toggle-sign', 'excel.insert-dcf-template'],
  视觉审计: ['excel.visualize-inputs', 'excel.visualize-formulas', 'excel.visualize-links', 'excel.clear-visualizations'],
  模型检查: ['excel.model-check'],
  公式追踪: ['excel.trace-precedents', 'excel.trace-dependents', 'excel.clear-trace'],
  工作簿: ['excel.optimize-workbook', 'excel.prepare-to-share', 'excel.names-manager'],
  跨应用: ['excel.link-to-powerpoint', 'excel.refresh-links'],
};

export function App() {
  const {
    backendBaseUrl, health, version, commands, isLoading, error,
    sidecarHealth, sidecarConnected, excelInfo, activePanel,
    refresh, checkSidecar, executeCommand, setActivePanel,
  } = useBridgeStore();

  const { isLoggedIn, user, login, logout } = useAuthStore();
  const [showOmnibar, setShowOmnibar] = useState(false);

  useEffect(() => {
    if (isLoggedIn) {
      void refresh();
      void checkSidecar();
    }
  }, [checkSidecar, isLoggedIn, refresh]);

  if (!isLoggedIn) {
    return <LoginPage onLogin={(token, userInfo) => login(token, userInfo)} />;
  }

  const navItems: { id: PanelId; label: string; icon: JSX.Element }[] = [
    { id: 'dashboard', label: '总览', icon: HomeIcon },
    { id: 'commands', label: '命令', icon: AppsIcon },
    { id: 'sidecar', label: 'Excel', icon: ExcelIcon },
    { id: 'ppt', label: 'PPT', icon: PptIcon },
    { id: 'word', label: 'Word', icon: WordIcon },
    { id: 'links', label: '链接', icon: LinkIcon },
    { id: 'audit', label: '审计', icon: AuditIcon },
    { id: 'aiwa', label: 'AIWA', icon: AiIcon },
    { id: 'admin', label: '管理', icon: AdminIcon },
  ];

  return (
    <main className="app-shell">
      <nav className="side-nav">
        <div className="nav-brand">ModelForge</div>
        {navItems.map((item) => (
          <button
            key={item.id}
            className={`nav-item${activePanel === item.id ? ' active' : ''}`}
            onClick={() => {
              recordUiAction({ action: 'nav.open', resourceId: item.id });
              setActivePanel(item.id);
            }}
          >
            <span className="nav-icon">{item.icon}</span>
            <span className="nav-label">{item.label}</span>
          </button>
        ))}
        <div className="nav-footer">
          {user && <Text size={100} style={{ color: '#888', textAlign: 'center' }}>{user.username}</Text>}
          <button className="nav-item" onClick={() => { recordUiAction({ action: 'app.refresh' }); void refresh(); void checkSidecar(); }} title="刷新">
            <span className="nav-icon"><Icon d="M17.65 6.35A7.96 7.96 0 0012 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08A5.99 5.99 0 0112 18c-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z" /></span>
          </button>
          <button className="nav-item" onClick={() => { recordUiAction({ action: 'auth.logout' }); logout(); }} title="退出登录">
            <span className="nav-icon"><Icon d="M16 13v-2H7V8l-5 4 5 4v-3z" /></span>
          </button>
        </div>
      </nav>

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
            onExecute={(commandId) => { void executeCommand(commandId); }}
          />
        )}

        {activePanel === 'sidecar' && (
          <SidecarPanel
            sidecarHealth={sidecarHealth}
            sidecarConnected={sidecarConnected}
            excelInfo={excelInfo}
            commands={commands}
          />
        )}

        {activePanel === 'ppt' && <DeckCheckViewer />}

        {activePanel === 'word' && <WordPanel />}

        {activePanel === 'links' && <LinkManager />}

        {activePanel === 'audit' && <AuditPanel commands={commands} />}

        {activePanel === 'aiwa' && <AiwaChat />}

        {activePanel === 'admin' && <AdminConsole />}
      </section>

      {showOmnibar && <Omnibar onClose={() => setShowOmnibar(false)} />}

      <button className="omnibar-trigger" onClick={() => { recordUiAction({ action: 'omnibar.open' }); setShowOmnibar(true); }} title="搜索命令 (Ctrl+Shift+P)">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
          <path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0016 9.5 6.5 6.5 0 109.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z" />
        </svg>
      </button>
    </main>
  );
}

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

function CommandPanel({ commands, onExecute }: {
  commands: CommandSummary[];
  onExecute: (commandId: string) => void;
}) {
  const commandMap = new Map(commands.map((command) => [command.id, command]));

  return (
    <div className="panel">
      <Title3>命令</Title3>
      {Object.entries(COMMAND_GROUPS).map(([group, commandIds]) => (
        <div key={group} className="command-group">
          <Text weight="semibold">{group}</Text>
          <div className="cmd-grid">
            {commandIds.map((commandId) => {
              const command = commandMap.get(commandId);
              if (!command) return null;
              return (
                <Button
                  key={commandId}
                  size="small"
                  onClick={() => onExecute(commandId)}
                  title={command.defaultShortcut ?? ''}
                >
                  {command.displayName}
                </Button>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

function SidecarPanel({ sidecarHealth, sidecarConnected, excelInfo, commands }: {
  sidecarHealth?: { service: string; timestampUtc: string };
  sidecarConnected: boolean;
  excelInfo?: { connected: boolean; workbook?: string; worksheet?: string; selection?: string };
  commands: CommandSummary[];
}) {
  const [shortcuts, setShortcuts] = useState<ShortcutItem[]>([]);
  const [shortcutError, setShortcutError] = useState<string | null>(null);
  const [shortcutMessage, setShortcutMessage] = useState<string | null>(null);
  const [shortcutLoading, setShortcutLoading] = useState(false);
  const [sidecarToken, setSidecarToken] = useState(getStoredSidecarToken);

  const commandNames = new Map(commands.map((command) => [command.id, command.displayName]));

  const loadShortcuts = async (audit = false) => {
    if (audit) {
      recordUiAction({ action: 'shortcut.refresh' });
    }
    setShortcutLoading(true);
    setShortcutError(null);
    try {
      setShortcuts(await sidecarClient.getShortcuts());
    } catch (error) {
      setShortcutError(error instanceof Error ? error.message : '加载快捷键失败。');
    } finally {
      setShortcutLoading(false);
    }
  };

  useEffect(() => {
    if (sidecarConnected) {
      void loadShortcuts();
    }
  }, [sidecarConnected]);

  const updateShortcut = (commandId: string, shortcut: string) => {
    setShortcuts((current) =>
      current.map((item) => item.commandId === commandId ? { ...item, shortcut } : item));
  };

  const saveShortcuts = async () => {
    recordUiAction({ action: 'shortcut.save', metadata: { count: shortcuts.length } });
    setShortcutError(null);
    setShortcutMessage(null);
    try {
      const result = await sidecarClient.importShortcuts({ shortcuts });
      setShortcuts(result.shortcuts);
      setShortcutMessage(`已保存 ${result.imported} 个快捷键。`);
    } catch (error) {
      setShortcutError(error instanceof Error ? error.message : '保存快捷键失败。');
    }
  };

  const exportShortcuts = async () => {
    recordUiAction({ action: 'shortcut.export', metadata: { count: shortcuts.length } });
    setShortcutError(null);
    setShortcutMessage(null);
    try {
      const data = await sidecarClient.exportShortcuts();
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `modelforge-shortcuts-${new Date().toISOString().slice(0, 10)}.json`;
      link.click();
      URL.revokeObjectURL(url);
      setShortcutMessage(`已导出 ${data.count} 个快捷键。`);
    } catch (error) {
      setShortcutError(error instanceof Error ? error.message : '导出快捷键失败。');
    }
  };

  const importShortcuts = async (file?: File) => {
    if (!file) return;
    recordUiAction({ action: 'shortcut.import', metadata: { fileName: file.name } });
    setShortcutError(null);
    setShortcutMessage(null);
    try {
      const importedShortcuts = parseShortcutImportPayload(await file.text());
      const result = await sidecarClient.importShortcuts({ shortcuts: importedShortcuts });
      setShortcuts(result.shortcuts);
      setShortcutMessage(`已导入 ${result.imported} 个快捷键。`);
    } catch (error) {
      setShortcutError(error instanceof Error ? error.message : '导入快捷键失败。');
    }
  };

  const saveSidecarToken = () => {
    recordUiAction({ action: 'sidecar.token.save', metadata: { tokenConfigured: !!sidecarToken.trim() } });
    saveStoredSidecarToken(sidecarToken);
    setShortcutMessage(sidecarToken.trim() ? 'Sidecar 本地令牌已保存。' : 'Sidecar 本地令牌已清除。');
    setShortcutError(null);
  };

  return (
    <div className="panel">
      <Title3>Excel 连接</Title3>
      <Card>
        <CardHeader header={<Text weight="semibold">Sidecar 服务</Text>} />
        <StatItem label="状态" value={sidecarConnected ? '运行中' : '未连接'} ok={sidecarConnected} />
        {sidecarHealth && <Text size={100}>服务: {sidecarHealth.service}</Text>}
      </Card>
      <Card>
        <CardHeader header={<Text weight="semibold">Sidecar 本地令牌</Text>} />
        <Text size={100}>仅在 Sidecar 配置了 LocalApiToken 时需要；令牌只保存在当前浏览器 localStorage。</Text>
        <div className="sidecar-token-row">
          <input
            className="sidecar-token-input"
            type="password"
            value={sidecarToken}
            onChange={(event) => setSidecarToken(event.currentTarget.value)}
            aria-label="Sidecar 本地 API 令牌"
            placeholder="留空表示不发送令牌"
          />
          <Button onClick={saveSidecarToken}>保存令牌</Button>
        </div>
      </Card>
      {excelInfo?.connected && (
        <Card>
          <CardHeader header={<Text weight="semibold">活动工作簿</Text>} />
          <StatItem label="工作簿" value={excelInfo.workbook ?? '-'} />
          <StatItem label="工作表" value={excelInfo.worksheet ?? '-'} />
          <StatItem label="选中区域" value={excelInfo.selection ?? '-'} />
        </Card>
      )}
      <Card>
        <CardHeader header={<Text weight="semibold">快捷键配置</Text>} />
        <Text size={100}>编辑后点击保存；导入/导出使用 JSON 格式。</Text>
        {shortcutError && <Text className="error-text">{shortcutError}</Text>}
        {shortcutMessage && <Text className="success-text">{shortcutMessage}</Text>}
        <div className="shortcut-toolbar">
          <Button onClick={() => void loadShortcuts(true)} disabled={!sidecarConnected || shortcutLoading}>
            {shortcutLoading ? '加载中...' : '刷新'}
          </Button>
          <Button appearance="primary" onClick={saveShortcuts} disabled={!sidecarConnected || shortcuts.length === 0}>
            保存
          </Button>
          <Button onClick={exportShortcuts} disabled={!sidecarConnected || shortcuts.length === 0}>导出 JSON</Button>
          <label className="shortcut-import-label">
            <input
              aria-label="导入快捷键 JSON"
              type="file"
              accept="application/json,.json"
              onChange={(event) => {
                void importShortcuts(event.currentTarget.files?.[0]);
                event.currentTarget.value = '';
              }}
            />
            导入 JSON
          </label>
        </div>
        <div className="shortcut-list">
          {shortcuts.map((shortcut) => (
            <div key={shortcut.commandId} className="shortcut-row">
              <div className="shortcut-meta">
                <Text weight="semibold">{commandNames.get(shortcut.commandId) ?? shortcut.displayName}</Text>
                <Text size={100}>{shortcut.commandId}</Text>
              </div>
              <input
                className="shortcut-input"
                value={shortcut.shortcut}
                onChange={(event) => updateShortcut(shortcut.commandId, event.currentTarget.value)}
                aria-label={`${shortcut.displayName} 快捷键`}
              />
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}

function WordPanel() {
  return (
    <div className="panel">
      <Title3>Word 工具</Title3>
      <Card>
        <CardHeader header={<Text weight="semibold">文档模板</Text>} />
        <Text>通过 Sidecar POST /api/execute 执行（host: word）：</Text>
        <ul>
          <li><Text>word.build-due-diligence</Text></li>
          <li><Text>word.build-cim</Text></li>
          <li><Text>word.build-management-presentation</Text></li>
          <li><Text>word.embed-excel-range</Text></li>
          <li><Text>word.refresh-links</Text></li>
        </ul>
      </Card>
    </div>
  );
}

function AuditPanel({ commands }: { commands: { id: string; displayName: string }[] }) {
  const auditCommands = commands.filter((command) =>
    command.id.startsWith('excel.visualize') ||
    command.id === 'excel.model-check' ||
    command.id === 'excel.clear-visualizations'
  );

  return (
    <div className="panel">
      <Title3>模型审计</Title3>
      <Card>
        <CardHeader header={<Text weight="semibold">审计命令</Text>} />
        <ul>
          {auditCommands.map((command) => (
            <li key={command.id}>
              <Text>{command.displayName}</Text> <Text size={100}>({command.id})</Text>
            </li>
          ))}
        </ul>
      </Card>
    </div>
  );
}

export const __appTestables = {
  parseShortcutImportPayload,
  getStoredSidecarToken,
  saveStoredSidecarToken,
};
