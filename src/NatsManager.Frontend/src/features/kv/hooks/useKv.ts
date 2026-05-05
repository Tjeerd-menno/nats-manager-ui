import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { queryKeys } from '../../../api/queryKeys';
import type { ListResponse } from '../../../api/types';
import type { KvBucketInfo, KvEntry, KvKeyHistoryEntry, CreateKvBucketRequest, PutKvKeyRequest } from '../types';

export function useKvBuckets(environmentId: string | null) {
  return useQuery({
    queryKey: queryKeys.kvBuckets(environmentId),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.kvBuckets(environmentId));
      return response.data as ListResponse<KvBucketInfo>;
    },
    enabled: !!environmentId,
  });
}

export function useKvBucket(environmentId: string | null, bucketName: string | undefined) {
  return useQuery({
    queryKey: queryKeys.kvBucket(environmentId, bucketName),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.kvBucket(environmentId, bucketName));
      return response.data as KvBucketInfo;
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useKvKeys(environmentId: string | null, bucketName: string | undefined, search?: string) {
  return useQuery({
    queryKey: queryKeys.kvKeys(environmentId, bucketName, search),
    queryFn: async () => {
      const params: Record<string, string> = {};
      if (search) params.search = search;
      const response = await apiClient.get(apiEndpoints.kvKeys(environmentId, bucketName), { params });
      return response.data as ListResponse<KvEntry>;
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useKvKey(environmentId: string | null, bucketName: string | undefined, key: string | undefined) {
  return useQuery({
    queryKey: queryKeys.kvKey(environmentId, bucketName, key),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.kvKey(environmentId, bucketName, key));
      return response.data as KvEntry;
    },
    enabled: !!environmentId && !!bucketName && !!key,
  });
}

export function useKvKeyHistory(environmentId: string | null, bucketName: string | undefined, key: string | undefined) {
  return useQuery({
    queryKey: queryKeys.kvKeyHistory(environmentId, bucketName, key),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.kvKeyHistory(environmentId, bucketName, key));
      return response.data as ListResponse<KvKeyHistoryEntry>;
    },
    enabled: !!environmentId && !!bucketName && !!key,
  });
}

export function useCreateKvBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateKvBucketRequest) => {
      await apiClient.post(apiEndpoints.kvBuckets(environmentId), data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.kvBuckets(environmentId) });
    },
  });
}

export function useDeleteKvBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bucketName: string) => {
      await apiClient.delete(apiEndpoints.kvBucket(environmentId, bucketName), {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.kvBuckets(environmentId) });
    },
  });
}

export function usePutKvKey(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ key, ...data }: PutKvKeyRequest & { key: string }) => {
      const response = await apiClient.put(apiEndpoints.kvKey(environmentId, bucketName, key), data);
      return response.data as { revision: number };
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.kvKeys(environmentId, bucketName) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvBucket(environmentId, bucketName) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvBuckets(environmentId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvKey(environmentId, bucketName, variables.key) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvKeyHistory(environmentId, bucketName, variables.key) });
    },
  });
}

export function useDeleteKvKey(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (key: string) => {
      await apiClient.delete(apiEndpoints.kvKey(environmentId, bucketName, key), {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: (_data, key) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.kvKeys(environmentId, bucketName) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvBucket(environmentId, bucketName) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvBuckets(environmentId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvKey(environmentId, bucketName, key) });
      queryClient.invalidateQueries({ queryKey: queryKeys.kvKeyHistory(environmentId, bucketName, key) });
    },
  });
}
