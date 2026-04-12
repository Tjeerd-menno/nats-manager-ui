import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { KvBucketInfo, KvEntry, KvKeyHistoryEntry, CreateKvBucketRequest, PutKvKeyRequest } from '../types';

export function useKvBuckets(environmentId: string | null) {
  return useQuery({
    queryKey: ['kv-buckets', environmentId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/kv/buckets`);
      return response.data as KvBucketInfo[];
    },
    enabled: !!environmentId,
  });
}

export function useKvBucket(environmentId: string | null, bucketName: string | undefined) {
  return useQuery({
    queryKey: ['kv-buckets', environmentId, bucketName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/kv/buckets/${bucketName}`);
      return response.data as KvBucketInfo;
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useKvKeys(environmentId: string | null, bucketName: string | undefined, search?: string) {
  return useQuery({
    queryKey: ['kv-keys', environmentId, bucketName, search],
    queryFn: async () => {
      const params: Record<string, string> = {};
      if (search) params.search = search;
      const response = await apiClient.get(`/environments/${environmentId}/kv/buckets/${bucketName}/keys`, { params });
      return response.data as { items: KvEntry[] };
    },
    enabled: !!environmentId && !!bucketName,
  });
}

export function useKvKey(environmentId: string | null, bucketName: string | undefined, key: string | undefined) {
  return useQuery({
    queryKey: ['kv-key', environmentId, bucketName, key],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/kv/buckets/${bucketName}/keys/${key}`);
      return response.data as KvEntry;
    },
    enabled: !!environmentId && !!bucketName && !!key,
  });
}

export function useKvKeyHistory(environmentId: string | null, bucketName: string | undefined, key: string | undefined) {
  return useQuery({
    queryKey: ['kv-key-history', environmentId, bucketName, key],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/kv/buckets/${bucketName}/keys/${key}/history`);
      return response.data as { entries: KvKeyHistoryEntry[] };
    },
    enabled: !!environmentId && !!bucketName && !!key,
  });
}

export function useCreateKvBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateKvBucketRequest) => {
      await apiClient.post(`/environments/${environmentId}/kv/buckets`, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kv-buckets', environmentId] });
    },
  });
}

export function useDeleteKvBucket(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bucketName: string) => {
      await apiClient.delete(`/environments/${environmentId}/kv/buckets/${bucketName}`, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kv-buckets', environmentId] });
    },
  });
}

export function usePutKvKey(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ key, ...data }: PutKvKeyRequest & { key: string }) => {
      const response = await apiClient.put(`/environments/${environmentId}/kv/buckets/${bucketName}/keys/${key}`, data);
      return response.data as { revision: number };
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kv-keys', environmentId, bucketName] });
      void queryClient.invalidateQueries({ queryKey: ['kv-key', environmentId, bucketName] });
      void queryClient.invalidateQueries({ queryKey: ['kv-key-history', environmentId, bucketName] });
    },
  });
}

export function useDeleteKvKey(environmentId: string | null, bucketName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (key: string) => {
      await apiClient.delete(`/environments/${environmentId}/kv/buckets/${bucketName}/keys/${key}`, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kv-keys', environmentId, bucketName] });
      void queryClient.invalidateQueries({ queryKey: ['kv-key', environmentId, bucketName] });
    },
  });
}
