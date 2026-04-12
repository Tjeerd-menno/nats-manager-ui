import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient, extractDataFreshness } from '../../../api/client';
import type { PaginatedResult, PaginatedQueryParams, DataFreshness } from '../../../api/types';
import type {
  EnvironmentListItem,
  EnvironmentDetail,
  RegisterEnvironmentRequest,
  RegisterEnvironmentResult,
  UpdateEnvironmentRequest,
  TestConnectionResult,
} from '../types';

const ENVIRONMENTS_KEY = 'environments';

export function useEnvironments(params?: PaginatedQueryParams) {
  return useQuery({
    queryKey: [ENVIRONMENTS_KEY, params],
    queryFn: async (): Promise<{ data: PaginatedResult<EnvironmentListItem>; freshness: DataFreshness }> => {
      const response = await apiClient.get('/environments', { params });
      return {
        data: response.data as PaginatedResult<EnvironmentListItem>,
        freshness: extractDataFreshness(response.headers as Record<string, string>),
      };
    },
  });
}

export function useEnvironment(id: string | undefined) {
  return useQuery({
    queryKey: [ENVIRONMENTS_KEY, id],
    queryFn: async (): Promise<EnvironmentDetail> => {
      const response = await apiClient.get(`/environments/${id}`);
      return response.data as EnvironmentDetail;
    },
    enabled: !!id,
  });
}

export function useRegisterEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: RegisterEnvironmentRequest): Promise<RegisterEnvironmentResult> => {
      const response = await apiClient.post('/environments', data);
      return response.data as RegisterEnvironmentResult;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [ENVIRONMENTS_KEY] });
    },
  });
}

export function useUpdateEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: UpdateEnvironmentRequest & { id: string }) => {
      await apiClient.put(`/environments/${id}`, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [ENVIRONMENTS_KEY] });
    },
  });
}

export function useDeleteEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/environments/${id}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [ENVIRONMENTS_KEY] });
    },
  });
}

export function useTestConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string): Promise<TestConnectionResult> => {
      const response = await apiClient.post(`/environments/${id}/test`);
      return response.data as TestConnectionResult;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [ENVIRONMENTS_KEY] });
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}

export function useEnableDisableEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, enable }: { id: string; enable: boolean }) => {
      await apiClient.post(`/environments/${id}/enable`, { enable });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [ENVIRONMENTS_KEY] });
    },
  });
}
