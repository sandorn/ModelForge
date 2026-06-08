import { useState, useMemo, useCallback } from 'react';
import Fuse from 'fuse.js';
import { Input, Text } from '@fluentui/react-components';
import { useBridgeStore } from '../services/bridgeStore';

interface SearchItem {
  id: string;
  displayName: string;
  category: string;
  shortcut?: string;
  description?: string;
  hostLabel: string;
  isNative?: boolean;
}

const HISTORY_KEY = 'modelforge_omnibar_history';
const MAX_HISTORY = 20;

function loadHistory(): string[] {
  try {
    const raw = localStorage.getItem(HISTORY_KEY);
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

function saveHistory(ids: string[]) {
  localStorage.setItem(HISTORY_KEY, JSON.stringify(ids.slice(0, MAX_HISTORY)));
}

/** 常用 Office 原生命令 */
const OFFICE_NATIVE_COMMANDS: SearchItem[] = [
  // Excel
  { id: 'native:excel.PasteSpecial', displayName: '选择性粘贴', category: 'Excel 原生', hostLabel: 'Excel', isNative: true, description: 'Alt+E+S' },
  { id: 'native:excel.GoToSpecial', displayName: '定位条件', category: 'Excel 原生', hostLabel: 'Excel', isNative: true, description: 'Ctrl+G → Special' },
  { id: 'native:excel.AutoFilter', displayName: '自动筛选', category: 'Excel 原生', hostLabel: 'Excel', isNative: true, description: 'Ctrl+Shift+L' },
  { id: 'native:excel.FormatCells', displayName: '设置单元格格式', category: 'Excel 原生', hostLabel: 'Excel', isNative: true, description: 'Ctrl+1' },
  { id: 'native:excel.NameManager', displayName: '名称管理器', category: 'Excel 原生', hostLabel: 'Excel', isNative: true, description: 'Ctrl+F3' },
  { id: 'native:excel.PivotTable', displayName: '插入数据透视表', category: 'Excel 原生', hostLabel: 'Excel', isNative: true },
  { id: 'native:excel.DataValidation', displayName: '数据验证', category: 'Excel 原生', hostLabel: 'Excel', isNative: true },
  { id: 'native:excel.ConditionalFormatting', displayName: '条件格式', category: 'Excel 原生', hostLabel: 'Excel', isNative: true },
  { id: 'native:excel.RemoveDuplicates', displayName: '删除重复值', category: 'Excel 原生', hostLabel: 'Excel', isNative: true },
  { id: 'native:excel.TextToColumns', displayName: '分列', category: 'Excel 原生', hostLabel: 'Excel', isNative: true },
  // PowerPoint
  { id: 'native:ppt.SlideMaster', displayName: '幻灯片母版', category: 'PPT 原生', hostLabel: 'PPT', isNative: true, description: 'View → Slide Master' },
  { id: 'native:ppt.SelectionPane', displayName: '选择窗格', category: 'PPT 原生', hostLabel: 'PPT', isNative: true, description: 'Alt+F10' },
  { id: 'native:ppt.FormatShape', displayName: '设置形状格式', category: 'PPT 原生', hostLabel: 'PPT', isNative: true },
  { id: 'native:ppt.GridAndGuides', displayName: '网格和参考线', category: 'PPT 原生', hostLabel: 'PPT', isNative: true },
  // Word
  { id: 'native:word.NavigationPane', displayName: '导航窗格', category: 'Word 原生', hostLabel: 'Word', isNative: true, description: 'Ctrl+F' },
  { id: 'native:word.StylesPane', displayName: '样式窗格', category: 'Word 原生', hostLabel: 'Word', isNative: true, description: 'Alt+Ctrl+Shift+S' },
];

/**
 * Omnibar — 全局命令搜索栏。
 * 搜索 ModelForge 命令 + Office 原生命令提示，支持 host 感知过滤和搜索历史。
 */
export function Omnibar({ onClose }: { onClose: () => void }) {
  const { commands, executeCommand, sidecarConnected } = useBridgeStore();
  const [query, setQuery] = useState('');
  const [selectedIdx, setSelectedIdx] = useState(0);
  const [history, setHistory] = useState<string[]>(loadHistory);

  // 自动检测当前宿主
  const currentHost = useMemo(() => {
    if (sidecarConnected) return 'excel'; // Sidecar 连接时假设 Excel 活动
    // 可通过 URL 或 Office.js 上下文判断，当前简化
    return '';
  }, [sidecarConnected]);

  const modelForgeItems: SearchItem[] = useMemo(() =>
    commands.map(c => {
      const hostLabel = c.host === 1 ? 'Excel' : c.host === 2 ? 'PPT' : c.host === 3 ? 'Word' : 'Web';
      return {
        id: c.id,
        displayName: c.displayName,
        category: c.category ?? 'ModelForge',
        shortcut: c.defaultShortcut ?? undefined,
        description: c.description ?? undefined,
        hostLabel,
      };
    }),
    [commands]
  );

  const allItems = useMemo(() => [...modelForgeItems, ...OFFICE_NATIVE_COMMANDS], [modelForgeItems]);

  const fuse = useMemo(() => new Fuse(allItems, {
    keys: ['displayName', 'category', 'id', 'description', 'hostLabel'],
    threshold: 0.4,
  }), [allItems]);

  const results = useMemo(() => {
    if (!query.trim()) {
      // 空查询：显示历史 + 当前 host 相关命令
      const historyItems = history
        .map(id => allItems.find(item => item.id === id))
        .filter((item): item is SearchItem => !!item);
      const hostItems = currentHost
        ? allItems.filter(item => item.hostLabel.toLowerCase() === currentHost && !history.includes(item.id))
        : [];
      // 去重
      const seen = new Set(history);
      const rest = hostItems.filter(item => !seen.has(item.id));
      return [...historyItems, ...rest].slice(0, 12);
    }
    return fuse.search(query).map(r => r.item);
  }, [query, fuse, allItems, history, currentHost]);

  const execute = useCallback((item: SearchItem) => {
    // 更新历史
    const next = [item.id, ...history.filter(id => id !== item.id)];
    setHistory(next);
    saveHistory(next);

    if (item.isNative) {
      // Office 原生命令：显示为提示（暂不自动执行）
      onClose();
      return;
    }
    void executeCommand(item.id, item.hostLabel.toLowerCase());
    onClose();
  }, [executeCommand, onClose, history]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIdx(i => Math.min(i + 1, results.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIdx(i => Math.max(i - 1, 0));
    } else if (e.key === 'Enter' && results[selectedIdx]) {
      execute(results[selectedIdx]);
    } else if (e.key === 'Escape') {
      onClose();
    }
  };

  return (
    <div className="omnibar-overlay" onClick={onClose}>
      <div className="omnibar-modal" onClick={e => e.stopPropagation()}>
        <Input
          autoFocus
          placeholder={currentHost ? `搜索命令... 当前: ${currentHost.toUpperCase()}` : '搜索命令...'}
          value={query}
          onChange={(_, d) => { setQuery(d.value); setSelectedIdx(0); }}
          onKeyDown={handleKeyDown}
          style={{ width: '100%' }}
        />
        <div className="omnibar-results">
          {results.slice(0, 12).map((item, i) => (
            <button
              key={item.id}
              className={`omnibar-item${i === selectedIdx ? ' selected' : ''}`}
              onClick={() => execute(item)}
            >
              <div className="omnibar-item-main">
                <Text weight="semibold">{item.displayName}</Text>
                <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                  {item.shortcut && (
                    <Text size={100} className="omnibar-shortcut">{item.shortcut}</Text>
                  )}
                  {item.isNative && (
                    <span className="omnibar-native-badge" style={{
                      fontSize: '10px',
                      background: '#e8e8e8',
                      padding: '1px 5px',
                      borderRadius: '3px',
                      color: '#666',
                    }}>Office</span>
                  )}
                </div>
              </div>
              <Text size={100}>
                <span className="omnibar-host">{item.hostLabel}</span> {item.category} · {item.id}
              </Text>
            </button>
          ))}
          {results.length === 0 && (
            <Text size={200} className="omnibar-empty">无匹配命令</Text>
          )}
        </div>
      </div>
    </div>
  );
}
