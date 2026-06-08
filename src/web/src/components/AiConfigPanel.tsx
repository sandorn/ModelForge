import { useEffect, useState } from 'react';
import { Button, Card, CardHeader, Spinner, Text, Title3, Badge } from '@fluentui/react-components';
import { apiClient } from '../services/apiClient';
import { recordUiAction } from '../services/uiAudit';

interface AiConfig {
  provider: string;
  model: string;
  modes: string[];
}

export function AiConfigPanel() {
  const [config, setConfig] = useState<AiConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [testResult, setTestResult] = useState<string | null>(null);
  const [testLoading, setTestLoading] = useState(false);

  const loadConfig = async () => {
    setLoading(true);
    setError(null);
    try {
      setConfig(await apiClient.getAiwaConfig());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load AI config.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void loadConfig(); }, []);

  const testConnection = async () => {
    recordUiAction({ action: 'aiwa.test' });
    setTestLoading(true);
    setTestResult(null);
    try {
      const result = await apiClient.sendAiwaMessage({ message: 'Hello, respond with just "OK".', mode: 'chat' });
      setTestResult(`Success (${result.model}${result.fallbackMock ? ' - fallback mock' : ''}): ${result.response.substring(0, 200)}`);
    } catch (err) {
      setTestResult(`Error: ${err instanceof Error ? err.message : 'Unknown error'}`);
    } finally {
      setTestLoading(false);
    }
  };

  if (loading) return <Spinner label="Loading AI config..." />;

  const providerLabel = config?.provider === 'openai-compatible' ? 'Agnes / OpenAI Compatible'
    : config?.provider === 'ollama' ? 'Ollama (Local)'
    : 'Mock (No AI backend)';

  const providerColor = config?.provider === 'openai-compatible' ? 'success'
    : config?.provider === 'ollama' ? 'brand'
    : 'warning';

  return (
    <div className="panel">
      <Title3>AI 模型配置</Title3>

      {error && <Text className="error-text">{error}</Text>}

      <div className="card-grid" style={{ marginTop: '1rem' }}>
        <Card>
          <CardHeader header={<Text weight="semibold">当前 Provider</Text>} />
          <Badge appearance="filled" color={providerColor} style={{ marginBottom: '0.5rem' }}>{providerLabel}</Badge>
          <Text size={200} block>Model: {config?.model ?? 'N/A'}</Text>
          <Text size={100} block style={{ color: '#666', marginTop: '4px' }}>Provider: {config?.provider}</Text>
        </Card>

        <Card>
          <CardHeader header={<Text weight="semibold">可用模式</Text>} />
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '4px' }}>
            {(config?.modes ?? []).map(mode => (
              <Badge key={mode} appearance="tint">{mode}</Badge>
            ))}
          </div>
          <Text size={100} style={{ marginTop: '8px', display: 'block', color: '#666' }}>
            总结 / 展开 / 改写 / 校对 / 翻译 / 公式解释
          </Text>
        </Card>

        <Card>
          <CardHeader header={<Text weight="semibold">连接测试</Text>} />
          <Button appearance="primary" onClick={() => void testConnection()} disabled={testLoading}>
            {testLoading ? '测试中...' : '测试连接'}
          </Button>
          {testResult && (
            <Text size={200} style={{ marginTop: '8px', whiteSpace: 'pre-wrap' }}>{testResult}</Text>
          )}
        </Card>

        <Card>
          <CardHeader header={<Text weight="semibold">配置方式</Text>} />
          <Text size={200} style={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap', lineHeight: '1.8' }}>
{`# .env 或环境变量:

# Agnes / OpenAI-compatible:
AIWA__Provider=openai-compatible
AIWA__ApiUrl=https://apihub.agnes-ai.com
AIWA__ApiKey=sk-xxx
AIWA__Model=agnes-2.0-flash

# Ollama (本地):
AIWA__Provider=ollama
AIWA__ApiUrl=http://localhost:11434
AIWA__Model=llama3

# Mock (默认，无需 API):
AIWA__Provider=mock`}
          </Text>
        </Card>
      </div>

      <div style={{ marginTop: '1rem' }}>
        <Button size="small" onClick={() => void loadConfig()}>刷新配置</Button>
      </div>
    </div>
  );
}
