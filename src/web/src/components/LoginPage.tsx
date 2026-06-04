import { useState } from 'react';
import { Button, Input, Text, Title3, Card, CardHeader } from '@fluentui/react-components';

interface LoginPageProps {
  onLogin: (token: string, user: { userId: string; username: string; role: string }) => void;
}

/**
 * 登录页面组件。
 * 通过后端 /api/auth/login 获取 JWT Token。
 */
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
      const res = await fetch('http://localhost:5095/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: username.trim(), password }),
      });

      if (!res.ok) {
        setError('登录失败：用户名或密码错误。');
        setLoading(false);
        return;
      }

      const data = await res.json();
      localStorage.setItem('modelforge_token', data.token);
      localStorage.setItem('modelforge_user', JSON.stringify({
        userId: data.userId,
        username: data.username,
        role: data.role,
      }));

      onLogin(data.token, { userId: data.userId, username: data.username, role: data.role });
    } catch {
      setError('无法连接到后端服务。请确认 Backend 已启动 (localhost:5095)。');
    }
    setLoading(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') void handleLogin();
  };

  return (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      height: '100vh', background: '#f5f7fb',
    }}>
      <Card style={{ width: 340, padding: 24 }}>
        <CardHeader header={<Title3>ModelForge 登录</Title3>} />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <Input
            placeholder="用户名"
            value={username}
            onChange={(_, d) => setUsername(d.value)}
            onKeyDown={handleKeyDown}
            autoFocus
          />
          <Input
            type="password"
            placeholder="密码"
            value={password}
            onChange={(_, d) => setPassword(d.value)}
            onKeyDown={handleKeyDown}
          />
          {error && <Text size={200} style={{ color: '#b10e1c' }}>{error}</Text>}
          <Button appearance="primary" onClick={() => void handleLogin()} disabled={loading}>
            {loading ? '登录中...' : '登录'}
          </Button>
          <Text size={100} style={{ color: '#888' }}>
            默认账号: admin / analyst / auditor (密码: 用户名 + 123)
          </Text>
        </div>
      </Card>
    </div>
  );
}
