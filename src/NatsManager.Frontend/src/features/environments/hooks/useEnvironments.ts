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
import { syncEnvironmentStatus, syncEnvironmentStatuses } from '../../../read-model/sync/environment-status-sync';

export function useEnvironments(params?: PaginatedQueryParams) {
  return useQuery({
    queryKey: queryKeys.environmentList(params),
    queryFn: async (): Promise<{ data: PaginatedResult<EnvironmentListItem>; freshness: DataFreshness }> => {
      const response = await apiClient.get(apiEndpoints.environments(), { params });
      const data = response.data as PaginatedResult<EnvironmentListItem>;
      syncEnvironmentStatuses(data.items);
      return {
        data,
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
      const data = response.data as EnvironmentDetail;
      syncEnvironmentStatus(data);
      return data;
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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.environments() }),
  });
}

export function useUpdateEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...data }: UpdateEnvironmentRequest & { id: string }) => {
      await apiClient.put(apiEndpoints.environmentDetail(id), data);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.environments() }),
  });
}

export function useDeleteEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(apiEndpoints.environmentDetail(id));
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.environments() }),
  });
}

export function useTestConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string): Promise<TestConnectionResult> => {
      const response = await apiClient.post(apiEndpoints.environmentTest(id));
      return response.data as TestConnectionResult;
    },
    onSuccess: () => Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.environments() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboardRoot() }),
    ]),
  });
}

export function useEnableDisableEnvironment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, enable }: { id: string; enable: boolean }) => {
      await apiClient.post(apiEndpoints.environmentEnable(id), { enable });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.environments() }),
  });
}
