import { describe, expect, it } from 'vitest';
import { __loginPageTestables } from '../components/LoginPage';

describe('LoginPage API envelope parsing', () => {
  it('unwraps Backend login ApiEnvelope', async () => {
    const response = new Response(JSON.stringify({
      traceId: 'trace-login',
      data: {
        token: 'jwt-token',
        userId: 'u1',
        username: 'admin',
        role: 'Admin',
        expiresAt: '2026-06-06T10:00:00Z',
      },
    }));

    const result = await __loginPageTestables.parseLoginResponse(response);

    expect(result.token).toBe('jwt-token');
    expect(result.username).toBe('admin');
  });

  it('throws readable message for invalid credentials', async () => {
    const response = new Response('', { status: 401 });

    await expect(__loginPageTestables.parseLoginResponse(response))
      .rejects.toThrow('登录失败：用户名或密码错误。');
  });

  it('throws envelope error when Backend returns one', async () => {
    const response = new Response(JSON.stringify({
      traceId: 'trace-login',
      error: '账号已停用。',
    }));

    await expect(__loginPageTestables.parseLoginResponse(response))
      .rejects.toThrow('账号已停用。');
  });

  it('rejects malformed successful login envelopes', async () => {
    const response = new Response(JSON.stringify({
      traceId: 'trace-login',
      data: {
        username: 'admin',
      },
    }));

    await expect(__loginPageTestables.parseLoginResponse(response))
      .rejects.toThrow('登录响应缺少 token。');
  });
});
