import { useState } from 'react';
import { Button, Card, CardHeader, Input, Text, Title3 } from '@fluentui/react-components';
import { apiClient } from '../services/apiClient';
import type { ApiEnvelope } from '../types/contracts';

interface AuthUser {
  userId: string;
  username: string;
  role: string;
}

interface LoginResponse extends AuthUser {
  token: string;
  expiresAt: string;
}

interface LoginPageProps {
  onLogin: (token: string, user: AuthUser) => void;
}

async function parseLoginResponse(response: Response): Promise<LoginResponse> {
  if (!response.ok) {
    throw new Error('登录失败：用户名或密码错误。');
  }

  const envelope = (await response.json()) as ApiEnvelope<LoginResponse>;
  if (envelope.error) {
    throw new Error(envelope.error);
  }
  if (!envelope.data?.token) {
    throw new Error('登录响应缺少 token。');
  }

  return envelope.data;
}

export function LoginPage({ onLogin }: LoginPageProps) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      setError('请输入用户名和密码。');
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await fetch(`${apiClient.getBackendBaseUrl()}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: username.trim(), password }),
      });

      const data = await parseLoginResponse(response);
      const user = {
        userId: data.userId,
        username: data.username,
        role: data.role,
      };

      localStorage.setItem('modelforge_token', data.token);
      localStorage.setItem('modelforge_user', JSON.stringify(user));
      onLogin(data.token, user);
    } catch (err) {
      setError(err instanceof Error ? err.message : '无法连接到后端服务。请确认 Backend 已启动 (localhost:5095)。');
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter') void handleLogin();
  };

  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      height: '100vh',
      background: '#f5f7fb',
    }}>
      <Card style={{ width: 340, padding: 24 }}>
        <CardHeader header={<Title3>ModelForge 登录</Title3>} />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <Input
            placeholder="用户名"
            value={username}
            onChange={(_, data) => setUsername(data.value)}
            onKeyDown={handleKeyDown}
            autoFocus
          />
          <Input
            type="password"
            placeholder="密码"
            value={password}
            onChange={(_, data) => setPassword(data.value)}
            onKeyDown={handleKeyDown}
          />
          {error && <Text size={200} style={{ color: '#b10e1c' }}>{error}</Text>}
          <Button appearance="primary" onClick={() => void handleLogin()} disabled={loading}>
            {loading ? '登录中...' : '登录'}
          </Button>
          <Text size={100} style={{ color: '#888' }}>
            ModelForge 仅供授权用户使用。请联系管理员获取账号。
          </Text>
        </div>
      </Card>
    </div>
  );
}

export const __loginPageTestables = {
  parseLoginResponse,
};
