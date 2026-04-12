export interface AuthUser {
  id: string;
  username: string;
  displayName: string;
  roles: string[];
}

export interface LoginRequest {
  username: string;
  password: string;
}
