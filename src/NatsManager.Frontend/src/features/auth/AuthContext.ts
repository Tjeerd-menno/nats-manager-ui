import { createContext } from 'react';
import type { AuthConfig, AuthUser, LoginRequest } from './types';

export interface AuthContextValue {
  user: AuthUser | null;
  authConfig: AuthConfig;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  loginWithOidc: () => void;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
