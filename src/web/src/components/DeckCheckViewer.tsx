import { useState } from 'react';
import {
  Button, Card, CardHeader, Text, Title3, Badge, Spinner,
  Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell,
} from '@fluentui/react-components';
import { sidecarClient } from '../services/sidecarClient';
import { recordUiAction } from '../services/uiAudit';

interface DeckIssue {
  slide: number;
  type: 'font' | 'term' | 'number' | 'density' | 'logo';
  message: string;
}

interface DeckReport {
  slidesScanned: number;
  fontIssues: number;
  termIssues: number;
  missingSlideNumbers: number;
  denseTextSlides: number;
  logoIssues: number;
  logoPositionIssues: number;
  templateName?: string;
  reportTitle?: string;
  brandPrimaryColor?: string;
  brandAccentColor?: string;
  totalIssues: number;
  overallStatus: 'Pass' | 'Review' | 'ActionRequired';
  reportPath?: string;
  issues: DeckIssue[];
}

function parseDeckCheckResult(raw: string): DeckReport | null {
  try {
    const data = JSON.parse(raw);
    if (!data || typeof data !== 'object') return null;

    const issues: DeckIssue[] = [];
    if (Array.isArray(data.Issues)) {
      for (const item of data.Issues) {
        const match = String(item).match(/Slide (\d+):\s*(.+)/);
        if (match) {
          const slideNum = parseInt(match[1], 10);
          const msg = match[2];
          let type: DeckIssue['type'] = 'term';
          if (msg.includes("字体") || msg.includes("Font") || msg.includes("font")) type = 'font';
          else if (msg.includes("缺失幻灯") || msg.includes("slide number") || msg.includes("编号")) type = 'number';
          else if (msg.includes("文本密度") || msg.includes("density") || msg.includes("字符")) type = 'density';
          else if (msg.includes("logo") || msg.includes("Logo")) type = 'logo';
          issues.push({ slide: slideNum, type, message: msg });
        }
      }
    }

    return {
      slidesScanned: data.SlidesScanned ?? 0,
      fontIssues: data.FontIssues ?? 0,
      termIssues: data.TermIssues ?? 0,
      missingSlideNumbers: data.MissingSlideNumbers ?? 0,
      denseTextSlides: data.DenseTextSlides ?? 0,
      logoIssues: data.LogoIssues ?? 0,
      logoPositionIssues: data.LogoPositionIssues ?? 0,
      templateName: data.TemplateName,
      reportTitle: data.ReportTitle,
      brandPrimaryColor: data.BrandPrimaryColor,
      brandAccentColor: data.BrandAccentColor,
      totalIssues: data.TotalIssues ?? issues.length,
      overallStatus: data.OverallStatus ?? (issues.length === 0 ? 'Pass' : 'Review'),
      reportPath: data.ReportPath,
      issues,
    };
  } catch {
    return null;
  }
}

const issueBadgeColor = (type: DeckIssue['type']) => {
  switch (type) {
    case 'font': return 'warning';
    case 'term': return 'danger';
    case 'number': return 'informative';
    case 'density': return 'important';
    case 'logo': return 'brand';
  }
};

const issueTypeLabel = (type: DeckIssue['type']) => {
  switch (type) {
    case 'font': return '字体';
    case 'term': return '术语';
    case 'number': return '编号';
    case 'density': return '密度';
    case 'logo': return 'Logo';
  }
};

export function DeckCheckViewer() {
  const [report, setReport] = useState<DeckReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runDeckCheck = async (exportPdf = false) => {
    recordUiAction({ action: exportPdf ? 'deck.export_pdf' : 'deck.check', commandId: 'ppt.deck-check' });
    setLoading(true);
    setError(null);
    try {
      const result = await sidecarClient.executeCommand({
        commandId: 'ppt.deck-check',
        host: 'powerpoint',
        arguments: exportPdf
          ? {
              exportPdf: 'true',
              checkLogos: 'true',
              templateName: 'ModelForge enterprise template',
              reportTitle: 'ModelForge Brand Compliance Report',
              brandPrimaryColor: '#1F3A5F',
              brandAccentColor: '#3B82F6',
            }
          : {
              checkLogos: 'true',
              templateName: 'ModelForge enterprise template',
              reportTitle: 'ModelForge Brand Compliance Report',
              brandPrimaryColor: '#1F3A5F',
              brandAccentColor: '#3B82F6',
            },
      });
      if (result.success && result.result) {
        const parsed = parseDeckCheckResult(result.result);
        if (parsed) {
          setReport(parsed);
        } else {
          setReport(null);
          setError('无法解析 Deck Check 结果');
        }
      } else {
        setError(result.message || '执行失败');
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : '连接 Sidecar 失败。请确认 PowerPoint 已启动。');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="panel">
      <Title3>Deck Check — 演示文稿审计</Title3>
      <Text size={200} style={{ color: '#666', display: 'block', marginBottom: 12 }}>
        扫描当前 PowerPoint 演示文稿的字体、术语、编号和文本密度合规性
      </Text>

      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        <Button appearance="primary" onClick={() => runDeckCheck(false)} disabled={loading}>
          {loading ? <Spinner size="tiny" /> : null}
          执行审计
        </Button>
        <Button onClick={() => runDeckCheck(true)} disabled={loading}>
          导出 PDF 报告
        </Button>
      </div>

      {error && (
        <Card style={{ marginTop: 12, borderLeft: '3px solid #d32f2f' }}>
          <Text style={{ color: '#d32f2f' }}>{error}</Text>
        </Card>
      )}

      {report && (
        <div style={{ marginTop: 16 }}>
          <Card style={{
            marginBottom: 16,
            borderLeft: `4px solid ${report.brandAccentColor ?? '#3B82F6'}`,
            background: report.overallStatus === 'Pass' ? '#f0f8f3' : report.overallStatus === 'Review' ? '#fff8e5' : '#fff1f0',
          }}>
            <Text weight="semibold">{report.reportTitle ?? 'ModelForge Brand Compliance Report'}</Text>
            <Text size={200} style={{ display: 'block', marginTop: 4 }}>
              状态：{report.overallStatus === 'Pass' ? '可分享' : report.overallStatus === 'Review' ? '需复核' : '需处理'} · 共 {report.totalIssues} 项问题
            </Text>
          </Card>

          {/* Summary cards */}
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 16 }}>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold">{report.slidesScanned}</Text>
              <Text size={100}>幻灯片已扫描</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold" style={{ color: report.fontIssues > 0 ? '#d32f2f' : '#2e7d32' }}>
                {report.fontIssues}
              </Text>
              <Text size={100}>字体问题</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold" style={{ color: report.termIssues > 0 ? '#d32f2f' : '#2e7d32' }}>
                {report.termIssues}
              </Text>
              <Text size={100}>术语违规</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold" style={{ color: report.missingSlideNumbers > 0 ? '#ed6c02' : '#2e7d32' }}>
                {report.missingSlideNumbers}
              </Text>
              <Text size={100}>缺少编号</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold" style={{ color: report.denseTextSlides > 0 ? '#ed6c02' : '#2e7d32' }}>
                {report.denseTextSlides}
              </Text>
              <Text size={100}>文本过密</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold" style={{ color: report.logoIssues > 0 ? '#ed6c02' : '#2e7d32' }}>
                {report.logoIssues}
              </Text>
              <Text size={100}>Logo 问题</Text>
            </Card>
            <Card style={{ flex: 1, minWidth: 100 }}>
              <Text size={300} weight="bold" style={{ color: report.logoPositionIssues > 0 ? '#ed6c02' : '#2e7d32' }}>
                {report.logoPositionIssues}
              </Text>
              <Text size={100}>Logo 位置</Text>
            </Card>
          </div>

          {report.templateName && (
            <Card style={{ marginBottom: 16 }}>
              <Text>企业模板：{report.templateName}</Text>
            </Card>
          )}

          {report.reportPath && (
            <Card style={{ marginBottom: 16, borderLeft: '3px solid #2e7d32' }}>
              <Text>PDF 报告已导出：{report.reportPath}</Text>
            </Card>
          )}

          {/* Issues table */}
          {report.issues.length > 0 && (
            <Card>
              <CardHeader header={<Text weight="semibold">发现问题 ({report.issues.length})</Text>} />
              <Table size="small">
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>幻灯片</TableHeaderCell>
                    <TableHeaderCell>类型</TableHeaderCell>
                    <TableHeaderCell>详情</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {report.issues.map((issue, i) => (
                    <TableRow key={i}>
                      <TableCell>Slide {issue.slide}</TableCell>
                      <TableCell>
                        <Badge appearance="filled" color={issueBadgeColor(issue.type)}>
                          {issueTypeLabel(issue.type)}
                        </Badge>
                      </TableCell>
                      <TableCell><Text size={100}>{issue.message}</Text></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Card>
          )}

          {report.issues.length === 0 && (
            <Card>
              <Text style={{ color: '#2e7d32' }}>✅ 未发现合规问题，演示文稿状态良好。</Text>
            </Card>
          )}
        </div>
      )}
    </div>
  );
}

export const __deckCheckViewerTestables = {
  parseDeckCheckResult,
};
