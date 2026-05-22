import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { apiClient } from '../../api/client';
import { AuthContext } from './AuthContext';
import type { AuthConfig, AuthUser, LoginRequest } from './types';

const defaultAuthConfig: AuthConfig = {
  oidcEnabled: false,
  oidcLoginPath: '/api/auth/oidc/login',
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [authConfig, setAuthConfig] = useState<AuthConfig>(defaultAuthConfig);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function loadAuthState() {
      const [configResult, userResult] = await Promise.allSettled([
        apiClient.get<AuthConfig>('/auth/config'),
        apiClient.get<AuthUser>('/auth/me'),
      ]);

      if (!isMounted) {
        return;
      }

      if (configResult.status === 'fulfilled') {
        setAuthConfig(configResult.value.data);
      }

      if (userResult.status === 'fulfilled') {
        setUser(userResult.value.data);
      } else {
        setUser(null);
      }

      setIsLoading(false);
    }

    void loadAuthState();

    return () => {
      isMounted = false;
    };
  }, []);

  const login = useCallback(async (credentials: LoginRequest) => {
    const res = await apiClient.post<AuthUser>('/auth/login', credentials);
    setUser(res.data);
  }, []);

  const loginWithOidc = useCallback(() => {
    const returnUrl = new URL('/dashboard', window.location.origin).toString();
    window.location.assign(`${authConfig.oidcLoginPath}?returnUrl=${encodeURIComponent(returnUrl)}`);
  }, [authConfig.oidcLoginPath]);

  const logout = useCallback(async () => {
    if (user?.authProvider === 'oidc') {
      const returnUrl = new URL('/login', window.location.origin).toString();
      window.location.assign(`/api/auth/oidc/logout?returnUrl=${encodeURIComponent(returnUrl)}`);
      setUser(null);
      return;
    }

    await apiClient.post('/auth/logout');
    setUser(null);
  }, [user?.authProvider]);

  const hasRole = useCallback(
    (role: string) => user?.roles.includes(role) ?? false,
    [user]
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        authConfig,
        isLoading,
        isAuthenticated: user !== null,
        login,
        loginWithOidc,
        logout,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
