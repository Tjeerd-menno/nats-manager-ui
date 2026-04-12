import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';

interface UserDto {
  id: string;
  username: string;
  displayName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

interface RoleDto {
  id: string;
  name: string;
  description: string;
}

interface UserRoleDto {
  assignmentId: string;
  roleId: string;
  roleName: string;
  environmentId: string | null;
  assignedAt: string;
}

export function useUsers() {
  return useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const response = await apiClient.get('/access-control/users');
      return response.data as UserDto[];
    },
  });
}

export function useRoles() {
  return useQuery({
    queryKey: ['roles'],
    queryFn: async () => {
      const response = await apiClient.get('/access-control/roles');
      return response.data as RoleDto[];
    },
  });
}

export function useUserRoles(userId: string | undefined) {
  return useQuery({
    queryKey: ['user-roles', userId],
    queryFn: async () => {
      const response = await apiClient.get(`/access-control/users/${userId}/roles`);
      return response.data as UserRoleDto[];
    },
    enabled: !!userId,
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: { username: string; displayName: string; password: string }) => {
      const response = await apiClient.post('/access-control/users', data);
      return response.data as { id: string };
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}

export function useDeactivateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (userId: string) => {
      await apiClient.delete(`/access-control/users/${userId}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}

export function useAssignRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ userId, roleId, environmentId }: { userId: string; roleId: string; environmentId?: string }) => {
      await apiClient.post(`/access-control/users/${userId}/roles`, { roleId, environmentId });
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['user-roles', variables.userId] });
    },
  });
}

export function useRevokeRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ userId, assignmentId }: { userId: string; assignmentId: string }) => {
      await apiClient.delete(`/access-control/users/${userId}/roles/${assignmentId}`);
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['user-roles', variables.userId] });
    },
  });
}
