import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient, extractDataFreshness } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { queryKeys } from '../../../api/queryKeys';
import type { PaginatedResult, PaginatedQueryParams, DataFreshness } from '../../../api/types';
import type {
  EnvironmentListItem,
  EnvironmentDetail,
  RegisterEnvironmentRequest,
  RegisterEnvironmentResult,
  UpdateEnvironmentRequest,
  TestConnectionResult,
} from '../types';

export function useEnvironments(params?: PaginatedQueryParams) {
  return useQuery({
    queryKey: queryKeys.environmentList(params),
    queryFn: async (): Promise<{ data: PaginatedResult<EnvironmentListItem>; freshness: DataFreshness }> => {
      const response = await apiClient.get(apiEndpoints.environments(), { params });
      return {
        data: response.data as PaginatedResult<EnvironmentListItem>,
        freshness: extractDataFreshness(response.headers as Record<string, string>),
      };
    },
  });
}

export function useEnvironment(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.environmentDetail(id),
    queryFn: async (): Promise<EnvironmentDetail> => {
      const response = await apiClient.get(apiEndpoints.environmentDetail(id));
      return response.data as EnvironmentDetail;
    },
    enabled: !!id,
  });
}

export function useRegisterEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: RegisterEnvironmentRequest): Promise<RegisterEnvironmentResult> => {
      const response = await apiClient.post(apiEndpoints.environments(), data);
      return response.data as RegisterEnvironmentResult;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.environments() });
    },
  });
}

export function useUpdateEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: UpdateEnvironmentRequest & { id: string }) => {
      await apiClient.put(apiEndpoints.environmentDetail(id), data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.environments() });
    },
  });
}

export function useDeleteEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(apiEndpoints.environmentDetail(id));
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.environments() });
    },
  });
}

export function useTestConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string): Promise<TestConnectionResult> => {
      const response = await apiClient.post(apiEndpoints.environmentTest(id));
      return response.data as TestConnectionResult;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.environments() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboardRoot() });
    },
  });
}

export function useEnableDisableEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, enable }: { id: string; enable: boolean }) => {
      await apiClient.post(apiEndpoints.environmentEnable(id), { enable });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.environments() });
    },
  });
}
