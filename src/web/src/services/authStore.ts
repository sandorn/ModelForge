import { create } from 'zustand';

interface AuthUser {
  userId: string;
  username: string;
  role: string;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  isLoggedIn: boolean;
  login: (token: string, user: AuthUser) => void;
  logout: () => void;
  getAuthHeaders: () => Record<string, string>;
}

export const useAuthStore = create<AuthState>((set, get) => {
  // 从 localStorage 恢复登录状态
  const storedToken = localStorage.getItem('modelforge_token');
  const storedUser = localStorage.getItem('modelforge_user');
  let initialUser: AuthUser | null = null;
  try { if (storedUser) initialUser = JSON.parse(storedUser); } catch {}

  return {
    token: storedToken,
    user: initialUser,
    isLoggedIn: !!storedToken && !!initialUser,

    login: (token: string, user: AuthUser) => {
      localStorage.setItem('modelforge_token', token);
      localStorage.setItem('modelforge_user', JSON.stringify(user));
      set({ token, user, isLoggedIn: true });
    },

    logout: () => {
      localStorage.removeItem('modelforge_token');
      localStorage.removeItem('modelforge_user');
      set({ token: null, user: null, isLoggedIn: false });
    },

    getAuthHeaders: (): Record<string, string> => {
      const t = get().token;
      if (t) return { Authorization: `Bearer ${t}` };
      return {};
    },
  };
});
