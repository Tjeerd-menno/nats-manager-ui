import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { queryKeys } from '../../../api/queryKeys';
import type { Role, User, UserRole } from '../types';

export function useUsers() {
  return useQuery({
    queryKey: queryKeys.users(),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.users());
      return response.data as User[];
    },
  });
}

export function useRoles() {
  return useQuery({
    queryKey: queryKeys.roles(),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.roles());
      return response.data as Role[];
    },
  });
}

export function useUserRoles(userId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.userRoles(userId),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.userRoles(userId));
      return response.data as UserRole[];
    },
    enabled: !!userId,
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: { username: string; displayName: string; password: string }) => {
      const response = await apiClient.post(apiEndpoints.users(), data);
      return response.data as { id: string };
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users() });
    },
  });
}

export function useDeactivateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (userId: string) => {
      await apiClient.delete(`${apiEndpoints.users()}/${userId}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.users() });
    },
  });
}

export function useAssignRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ userId, roleId, environmentId }: { userId: string; roleId: string; environmentId?: string }) => {
      await apiClient.post(apiEndpoints.userRoles(userId), { roleId, environmentId });
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.userRoles(variables.userId) });
    },
  });
}

export function useRevokeRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ userId, assignmentId }: { userId: string; assignmentId: string }) => {
      await apiClient.delete(apiEndpoints.userRole(userId, assignmentId));
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.userRoles(variables.userId) });
    },
  });
}
