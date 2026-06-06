import { useState } from 'react';
import {
  Button, Card, CardHeader, Text, Title3, Badge, Spinner,
  Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell,
} from '@fluentui/react-components';
import { sidecarClient } from '../services/sidecarClient';

interface NameInfo {
  name: string;
  refersTo: string;
  isVisible: boolean;
  isValid: boolean;
  error?: string;
}

interface NamesReport {
  allNames: NameInfo[];
  invalidNames: NameInfo[];
  totalCount: number;
  invalidCount: number;
  deletedCount?: number;
  deleteErrors?: string[];
}

function parseNamesResult(raw: string): NamesReport | null {
  try {
    const data = JSON.parse(raw);
    // Normalize camelCase from C# to camelCase TS
    return {
      allNames: (data.AllNames || data.allNames || []).map((n: any) => ({
        name: n.Name || n.name || '',
        refersTo: n.RefersTo || n.refersTo || '',
        isVisible: n.IsVisible ?? n.isVisible ?? true,
        isValid: n.IsValid ?? n.isValid ?? true,
        error: n.Error || n.error || undefined,
      })),
      invalidNames: (data.InvalidNames || data.invalidNames || []).map((n: any) => ({
        name: n.Name || n.name || '',
        refersTo: n.RefersTo || n.refersTo || '',
        isVisible: n.IsVisible ?? n.isVisible ?? true,
        isValid: false,
        error: n.Error || n.error || undefined,
      })),
      totalCount: data.TotalCount ?? data.totalCount ?? 0,
      invalidCount: data.InvalidCount ?? data.invalidCount ?? 0,
      deletedCount: data.DeletedCount ?? data.deletedCount,
      deleteErrors: data.DeleteErrors || data.deleteErrors || [],
    };
  } catch {
    return null;
  }
}

export function NamesManagerPanel() {
  const [report, setReport] = useState<NamesReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [action, setAction] = useState<'scan' | 'delete' | null>(null);

  const runScan = async () => {
    setLoading(true);
    setError(null);
    setAction('scan');
    try {
      const result = await sidecarClient.executeCommand({
        commandId: 'excel.names-manager',
        host: 'excel',
        arguments: { action: 'scan' },
      });
      if (result.success && result.result) {
        const parsed = parseNamesResult(result.result);
        if (parsed) {
          setReport(parsed);
        } else {
          setError('无法解析命名管理器结果');
        }
      } else {
        setError(result.message || '扫描失败');
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '连接 Sidecar 失败。请确认 Excel 已启动。');
    } finally {
      setLoading(false);
    }
  };

  const runDelete = async () => {
    if (!report || report.invalidCount === 0) return;
    setLoading(true);
    setError(null);
    setAction('delete');
    try {
      const result = await sidecarClient.executeCommand({
        commandId: 'excel.names-manager',
        host: 'excel',
        arguments: { action: 'delete' },
      });
      if (result.success && result.result) {
        const parsed = parseNamesResult(result.result);
        if (parsed) {
          setReport(parsed);
        }
      } else {
        setError(result.message || '删除失败');
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '删除操作失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="panel">
      <Title3>命名管理器 — Names Manager</Title3>
      <Text size={200} style={{ color: '#666', display: 'block', marginBottom: 12 }}>
        扫描工作簿中所有命名区域，检测无效引用，支持批量清理
      </Text>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <Button appearance="primary" onClick={runScan} disabled={loading}>
          {loading && action === 'scan' ? <Spinner size="tiny" /> : null}
          扫描命名区域
        </Button>
        {report && report.invalidCount > 0 && (
          <Button appearance="secondary" onClick={runDelete} disabled={loading}>
            {loading && action === 'delete' ? <Spinner size="tiny" /> : null}
            删除无效命名 ({report.invalidCount})
          </Button>
        )}
      </div>

      {error && (
        <Card style={{ marginTop: 12, borderLeft: '3px solid #d32f2f' }}>
          <Text style={{ color: '#d32f2f' }}>{error}</Text>
        </Card>
      )}

      {report && (
        <div style={{ marginTop: 16 }}>
          <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
            <Card style={{ flex: 1, minWidth: 80 }}>
              <Text size={300} weight="bold">{report.totalCount}</Text>
              <Text size={100}>命名总数</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 80 }}>
              <Text size={300} weight="bold" style={{ color: report.invalidCount > 0 ? '#d32f2f' : '#2e7d32' }}>
                {report.invalidCount}
              </Text>
              <Text size={100}>无效命名</Text>
            </Card>
            {report.deletedCount !== undefined && (
              <Card style={{ flex: 1, minWidth: 80 }}>
                <Text size={300} weight="bold" style={{ color: '#2e7d32' }}>
                  {report.deletedCount}
                </Text>
                <Text size={100}>已删除</Text>
              </Card>
            )}
          </div>

          {report.deleteErrors && report.deleteErrors.length > 0 && (
            <Card style={{ marginBottom: 12, borderLeft: '3px solid #ed6c02' }}>
              <Text weight="semibold">删除警告</Text>
              {report.deleteErrors.map((e, i) => (
                <Text key={i} size={100} style={{ display: 'block', color: '#ed6c02' }}>{e}</Text>
              ))}
            </Card>
          )}

          {report.allNames.length > 0 && (
            <Card>
              <CardHeader header={<Text weight="semibold">所有命名 ({report.allNames.length})</Text>} />
              <Table size="small">
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>名称</TableHeaderCell>
                    <TableHeaderCell>引用</TableHeaderCell>
                    <TableHeaderCell>状态</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {report.allNames.map((n, i) => (
                    <TableRow key={i} style={{ opacity: n.isValid ? 1 : 0.6 }}>
                      <TableCell>
                        <Text weight="semibold">{n.name}</Text>
                        {!n.isVisible && <Badge size="tiny" appearance="outline">隐藏</Badge>}
                      </TableCell>
                      <TableCell>
                        <Text size={100} style={{ fontFamily: 'monospace' }}>{n.refersTo || '—'}</Text>
                      </TableCell>
                      <TableCell>
                        {n.isValid ? (
                          <Badge appearance="filled" color="success">有效</Badge>
                        ) : (
                          <Badge appearance="filled" color="danger" title={n.error}>无效</Badge>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Card>
          )}

          {report.totalCount === 0 && (
            <Card>
              <Text style={{ color: '#666' }}>当前工作簿中没有命名区域。</Text>
            </Card>
          )}
        </div>
      )}
    </div>
  );
}
