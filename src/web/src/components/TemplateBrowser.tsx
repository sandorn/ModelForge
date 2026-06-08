import { useEffect, useState } from 'react';
import { Button, Card, CardHeader, Spinner, Text, Title3 } from '@fluentui/react-components';
import { sidecarClient } from '../services/sidecarClient';
import { recordUiAction } from '../services/uiAudit';

interface TemplateItem {
  name: string;
  fileName: string;
  rows: number;
  columns: number;
  savedAt: string;
}

export function TemplateBrowser() {
  const [templates, setTemplates] = useState<TemplateItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadTemplates = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await sidecarClient.executeCommand({ commandId: 'excel.list-templates', host: 'excel' });
      const data = JSON.parse(result.message ?? '{}') as { count: number; templates: TemplateItem[] };
      setTemplates(data.templates ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载模板失败。');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void loadTemplates(); }, []);

  const insert = async (templateName: string) => {
    recordUiAction({ action: 'template.insert', metadata: { templateName } });
    setError(null);
    setMessage(null);
    try {
      const result = await sidecarClient.executeCommand({ commandId: 'excel.insert-template', host: 'excel', arguments: { templateName } });
      setMessage(result.message ?? 'OK');
    } catch (err) {
      setError(err instanceof Error ? err.message : '插入模板失败。');
    }
  };

  const saveCurrent = async () => {
    const name = prompt('模板名称:');
    if (!name) return;
    recordUiAction({ action: 'template.save', metadata: { templateName: name } });
    setError(null);
    setMessage(null);
    try {
      const result = await sidecarClient.executeCommand({ commandId: 'excel.save-template', host: 'excel', arguments: { templateName: name } });
      setMessage(result.message ?? 'OK');
      await loadTemplates();
    } catch (err) {
      setError(err instanceof Error ? err.message : '保存模板失败。');
    }
  };

  if (loading) return <Spinner label="加载模板..." />;

  return (
    <div className="panel">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title3>模板库</Title3>
        <div style={{ display: 'flex', gap: '8px' }}>
          <Button size="small" onClick={() => void loadTemplates()}>刷新</Button>
          <Button size="small" appearance="primary" onClick={saveCurrent}>保存当前选区</Button>
        </div>
      </div>

      {error && <Text className="error-text">{error}</Text>}
      {message && <Text className="success-text">{message}</Text>}

      {templates.length === 0 ? (
        <Card style={{ marginTop: '1rem' }}>
          <CardHeader header={<Text weight="semibold">暂无模板</Text>} />
          <Text size={200}>在 Excel 中选中一个区域，点击"保存当前选区"来创建你的第一个模板。</Text>
        </Card>
      ) : (
        <div className="card-grid" style={{ marginTop: '1rem' }}>
          {templates.map((tpl) => (
            <Card key={tpl.fileName}>
              <CardHeader header={<Text weight="semibold">{tpl.name}</Text>} />
              <Text size={200}>{tpl.rows} 行 × {tpl.columns} 列</Text>
              <Text size={100}>{new Date(tpl.savedAt).toLocaleString()}</Text>
              <div style={{ marginTop: '8px' }}>
                <Button size="small" onClick={() => void insert(tpl.name)}>插入</Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
