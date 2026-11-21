export interface AuthUser {
  id: string;
  username: string;
  displayName: string;
  roles: string[];
  authProvider?: 'local' | 'oidc';
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthConfig {
  oidcEnabled: boolean;
  oidcLoginPath: string;
}
