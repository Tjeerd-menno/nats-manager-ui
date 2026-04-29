export interface User {
  id: string;
  username: string;
  displayName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface Role {
  id: string;
  name: string;
  description: string;
}

export interface UserRole {
  assignmentId: string;
  roleId: string;
  roleName: string;
  environmentId: string | null;
  assignedAt: string;
}
