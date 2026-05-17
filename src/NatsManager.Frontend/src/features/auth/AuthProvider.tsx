import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { apiClient } from '../../api/client';
import { AuthContext } from './AuthContext';
import type { AuthUser, LoginRequest } from './types';

interface AuthProviderProps {
  readonly children: ReactNode;
  readonly skipCurrentUserBootstrap?: boolean;
}

export function AuthProvider({
  children,
  skipCurrentUserBootstrap = false,
}: AuthProviderProps) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(() => !skipCurrentUserBootstrap);

  useEffect(() => {
    let isDisposed = false;

    if (skipCurrentUserBootstrap) {
      setIsLoading(false);
      return () => {
        isDisposed = true;
      };
    }

    setIsLoading(true);

    apiClient.get<AuthUser | null>('/auth/me')
      .then((res) => {
        if (!isDisposed) {
          setUser(res.data);
        }
      })
      .catch(() => {
        if (!isDisposed) {
          setUser(null);
        }
      })
      .finally(() => {
        if (!isDisposed) {
          setIsLoading(false);
        }
      });

    return () => {
      isDisposed = true;
    };
  }, [skipCurrentUserBootstrap]);

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
