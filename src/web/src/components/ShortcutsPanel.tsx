import { useMemo, useState } from 'react';
import { Input, Text, Title3, Card, CardHeader, Badge } from '@fluentui/react-components';
import { useBridgeStore } from '../services/bridgeStore';

export function ShortcutsPanel() {
  const { commands } = useBridgeStore();
  const [filter, setFilter] = useState('');

  const withShortcuts = useMemo(() =>
    commands.filter(c => c.defaultShortcut).sort((a, b) => a.category.localeCompare(b.category)),
    [commands]);

  const filtered = useMemo(() => {
    if (!filter.trim()) return withShortcuts;
    const q = filter.toLowerCase();
    return withShortcuts.filter(c =>
      c.id.toLowerCase().includes(q) ||
      c.displayName.toLowerCase().includes(q) ||
      (c.defaultShortcut ?? '').toLowerCase().includes(q) ||
      (c.category ?? '').toLowerCase().includes(q)
    );
  }, [withShortcuts, filter]);

  const categories = useMemo(() => {
    const cats = new Map<string, typeof filtered>();
    for (const cmd of filtered) {
      const cat = cmd.category || 'Other';
      if (!cats.has(cat)) cats.set(cat, []);
      cats.get(cat)!.push(cmd);
    }
    return cats;
  }, [filtered]);

  return (
    <div className="panel">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <Title3>快捷键参考 ({withShortcuts.length})</Title3>
      </div>
      <Input
        placeholder="搜索命令、快捷键或分类..."
        value={filter}
        onChange={(_, d) => setFilter(d.value)}
        style={{ marginBottom: '1rem', width: '100%' }}
      />
      {Array.from(categories.entries()).map(([category, cmds]) => (
        <Card key={category} style={{ marginBottom: '0.75rem' }}>
          <CardHeader header={<Text weight="semibold">{category}</Text>} />
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <tbody>
              {cmds.map(cmd => (
                <tr key={cmd.id} style={{ borderBottom: '1px solid #f0f0f0' }}>
                  <td style={{ padding: '4px 8px' }}><Text size={200}>{cmd.displayName}</Text></td>
                  <td style={{ padding: '4px 8px' }}>
                    <Badge appearance="filled" color="brand">{cmd.defaultShortcut}</Badge>
                  </td>
                  <td style={{ padding: '4px 8px' }}><Text size={100}>{cmd.id}</Text></td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      ))}
      {filtered.length === 0 && <Text size={200}>无匹配快捷键。</Text>}
    </div>
  );
}
