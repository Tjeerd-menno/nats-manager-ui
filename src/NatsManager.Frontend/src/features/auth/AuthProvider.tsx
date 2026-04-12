import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { apiClient } from '../../api/client';
import { AuthContext } from './AuthContext';
import type { AuthUser, LoginRequest } from './types';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    apiClient.get<AuthUser>('/auth/me')
      .then((res) => setUser(res.data))
      .catch(() => setUser(null))
      .finally(() => setIsLoading(false));
  }, []);

  const login = useCallback(async (credentials: LoginRequest) => {
    const res = await apiClient.post<AuthUser>('/auth/login', credentials);
    setUser(res.data);
  }, []);

  const logout = useCallback(async () => {
    await apiClient.post('/auth/logout');
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (role: string) => user?.roles.includes(role) ?? false,
    [user]
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: user !== null,
        login,
        logout,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
