import { useState, useMemo, useCallback } from 'react';
import Fuse from 'fuse.js';
import { Input, Text, Button } from '@fluentui/react-components';
import { useBridgeStore } from '../services/bridgeStore';

interface SearchItem {
  id: string;
  displayName: string;
  category: string;
  shortcut?: string;
  description?: string;
}

/**
 * Omnibar — 全局命令搜索栏（类 VS Code Ctrl+Shift+P）。
 * 支持模糊搜索和键盘导航，输入时实时过滤。
 */
export function Omnibar({ onClose }: { onClose: () => void }) {
  const { commands, executeCommand } = useBridgeStore();
  const [query, setQuery] = useState('');
  const [selectedIdx, setSelectedIdx] = useState(0);

  const items: SearchItem[] = useMemo(() =>
    commands.map(c => ({
      id: c.id,
      displayName: c.displayName,
      category: c.category ?? '',
      shortcut: c.defaultShortcut ?? undefined,
      description: c.description ?? undefined,
    })),
    [commands]
  );

  const fuse = useMemo(() => new Fuse(items, {
    keys: ['displayName', 'category', 'id', 'description'],
    threshold: 0.4,
  }), [items]);

  const results = useMemo(() => {
    if (!query.trim()) return items;
    return fuse.search(query).map(r => r.item);
  }, [query, fuse, items]);

  const execute = useCallback((item: SearchItem) => {
    void executeCommand(item.id);
    onClose();
  }, [executeCommand, onClose]);

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
          placeholder="搜索 ModelForge 命令..."
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
                {item.shortcut && (
                  <Text size={100} className="omnibar-shortcut">{item.shortcut}</Text>
                )}
              </div>
              <Text size={100}>{item.category} · {item.id}</Text>
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
