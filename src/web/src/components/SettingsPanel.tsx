import { useEffect, useState } from 'react';
import { Button, Card, CardHeader, Switch, Text, Title3, Input } from '@fluentui/react-components';
import { apiClient } from '../services/apiClient';
import { recordUiAction } from '../services/uiAudit';

interface SettingsState {
  sidecarToken: string;
  keyboardHookEnabled: boolean;
  defaultTemplate: string;
  saved: boolean;
  loading: boolean;
  error: string | null;
}

function getStored(field: string, fallback: string): string {
  return localStorage.getItem(`modelforge_${field}`) ?? fallback;
}

function setStored(field: string, value: string) {
  if (value) {
    localStorage.setItem(`modelforge_${field}`, value);
  } else {
    localStorage.removeItem(`modelforge_${field}`);
  }
}

export function SettingsPanel() {
  const [state, setState] = useState<SettingsState>({
    sidecarToken: getStored('sidecar_token', ''),
    keyboardHookEnabled: getStored('keyboard_hook', 'true') === 'true',
    defaultTemplate: getStored('default_template', 'DCF'),
    saved: false,
    loading: true,
    error: null,
  });

  useEffect(() => {
    const load = async () => {
      try {
        const config = await apiClient.getConfig('user-preferences');
        setState(prev => ({
          ...prev,
          keyboardHookEnabled: config.values?.['keyboardHookEnabled'] !== 'false',
          defaultTemplate: config.values?.['defaultTemplate'] ?? prev.defaultTemplate,
          loading: false,
        }));
      } catch {
        setState(prev => ({ ...prev, loading: false }));
      }
    };
    void load();
  }, []);

  const save = async () => {
    recordUiAction({ action: 'settings.save' });
    setState(prev => ({ ...prev, saved: false, error: null }));

    setStored('sidecar_token', state.sidecarToken);
    setStored('keyboard_hook', state.keyboardHookEnabled ? 'true' : 'false');
    setStored('default_template', state.defaultTemplate);

    try {
      await apiClient.upsertConfig('user-preferences', {
        values: {
          keyboardHookEnabled: state.keyboardHookEnabled ? 'true' : 'false',
          defaultTemplate: state.defaultTemplate,
        },
        updatedBy: 'web-ui',
      });
      setState(prev => ({ ...prev, saved: true }));
    } catch (err) {
      setState(prev => ({
        ...prev,
        error: err instanceof Error ? err.message : '保存设置失败。',
      }));
    }
  };

  return (
    <div className="panel">
      <Title3>偏好设置</Title3>

      {state.error && <Text className="error-text">{state.error}</Text>}
      {state.saved && <Text className="success-text">设置已保存。</Text>}

      <div className="card-grid">
        <Card>
          <CardHeader header={<Text weight="semibold">键盘钩子</Text>} />
          <Switch
            label={state.keyboardHookEnabled ? '已启用' : '已禁用'}
            checked={state.keyboardHookEnabled}
            onChange={(_, d) => setState(prev => ({ ...prev, keyboardHookEnabled: d.checked, saved: false }))}
          />
          <Text size={100}>启用全局快捷键（需要 Sidecar 以管理员权限运行）。</Text>
        </Card>

        <Card>
          <CardHeader header={<Text weight="semibold">默认模板</Text>} />
          <select
            value={state.defaultTemplate}
            onChange={e => setState(prev => ({ ...prev, defaultTemplate: e.target.value, saved: false }))}
            style={{ padding: '6px 8px', borderRadius: '4px', border: '1px solid #ccc', width: '100%' }}
          >
            <option value="DCF">DCF 估值模板</option>
            <option value="BlackScholes">Black-Scholes 期权定价</option>
          </select>
          <Text size={100}>使用"插入模板"命令时的默认模板类型。</Text>
        </Card>

        <Card>
          <CardHeader header={<Text weight="semibold">Sidecar 本地令牌</Text>} />
          <Input
            type="password"
            value={state.sidecarToken}
            onChange={(_, d) => setState(prev => ({ ...prev, sidecarToken: d.value, saved: false }))}
            placeholder="留空表示不发送令牌"
          />
          <Text size={100}>仅在 Sidecar 配置了 LocalApiToken 时需要。令牌保存在本地浏览器。</Text>
        </Card>

        <Card>
          <CardHeader header={<Text weight="semibold">后端连接</Text>} />
          <Text size={200}>API: {apiClient.getBackendBaseUrl()}</Text>
          <Text size={200}>设置同步到 Backend 'user-preferences' 配置 scope。</Text>
          {state.loading && <Text size={100}>正在加载远端设置...</Text>}
        </Card>
      </div>

      <div style={{ marginTop: '1rem' }}>
        <Button appearance="primary" onClick={save}>保存设置</Button>
      </div>
    </div>
  );
}
