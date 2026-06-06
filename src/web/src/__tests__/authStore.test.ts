import { describe, it, expect } from 'vitest';
import { useAuthStore } from '../services/authStore';

describe('authStore', () => {
  beforeEach(() => {
    useAuthStore.setState({
      token: null,
      user: null,
      isLoggedIn: false,
    });
  });

  it('initial state is unauthenticated', () => {
    const state = useAuthStore.getState();
    expect(state.isLoggedIn).toBe(false);
    expect(state.token).toBeNull();
    expect(state.user).toBeNull();
  });

  it('login sets token and user', () => {
    const { login } = useAuthStore.getState();
    login('test-token', { userId: 'u1', username: 'alice', role: 'Admin' });

    const state = useAuthStore.getState();
    expect(state.isLoggedIn).toBe(true);
    expect(state.token).toBe('test-token');
    expect(state.user).toEqual({ userId: 'u1', username: 'alice', role: 'Admin' });
  });

  it('logout clears state', () => {
    const { login, logout } = useAuthStore.getState();
    login('token', { userId: 'u1', username: 'alice', role: 'Admin' });
    logout();

    const state = useAuthStore.getState();
    expect(state.isLoggedIn).toBe(false);
    expect(state.token).toBeNull();
    expect(state.user).toBeNull();
  });

  it('getAuthHeaders returns Bearer header when logged in', () => {
    const { login, getAuthHeaders } = useAuthStore.getState();
    login('my-jwt', { userId: 'u1', username: 'alice', role: 'Admin' });

    const headers = getAuthHeaders();
    expect(headers).toEqual({ Authorization: 'Bearer my-jwt' });
  });

  it('getAuthHeaders returns empty when not logged in', () => {
    const { getAuthHeaders } = useAuthStore.getState();
    expect(getAuthHeaders()).toEqual({});
  });
});