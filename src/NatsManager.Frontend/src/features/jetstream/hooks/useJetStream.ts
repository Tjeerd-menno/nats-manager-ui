import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { queryKeys } from '../../../api/queryKeys';
import type { PaginatedResult, PaginatedQueryParams } from '../../../api/types';
import type { StreamListItem, StreamDetail, ConsumerInfo, CreateStreamRequest, UpdateStreamRequest, CreateConsumerRequest, StreamMessageItem } from '../types';

export function useStreams(environmentId: string | null, params?: PaginatedQueryParams) {
  return useQuery({
    queryKey: queryKeys.streams(environmentId, params),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.streamList(environmentId), { params });
      return response.data as PaginatedResult<StreamListItem>;
    },
    enabled: !!environmentId,
  });
}

export function useStream(environmentId: string | null, streamName: string | undefined) {
  return useQuery({
    queryKey: queryKeys.streamDetail(environmentId, streamName),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.streamDetail(environmentId, streamName));
      return response.data as StreamDetail;
    },
    enabled: !!environmentId && !!streamName,
  });
}

export function useConsumers(environmentId: string | null, streamName: string | undefined, params?: PaginatedQueryParams) {
  return useQuery({
    queryKey: queryKeys.consumers(environmentId, streamName, params),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.consumerList(environmentId, streamName), { params });
      return response.data as PaginatedResult<ConsumerInfo>;
    },
    enabled: !!environmentId && !!streamName,
  });
}

export function useConsumer(environmentId: string | null, streamName: string | undefined, consumerName: string | undefined) {
  return useQuery({
    queryKey: queryKeys.consumerDetail(environmentId, streamName, consumerName),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.consumerDetail(environmentId, streamName, consumerName));
      return response.data as ConsumerInfo;
    },
    enabled: !!environmentId && !!streamName && !!consumerName,
  });
}

export function useCreateStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateStreamRequest) => {
      await apiClient.post(apiEndpoints.streamList(environmentId), data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamsRoot(environmentId) });
    },
  });
}

export function useUpdateStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ name, ...data }: UpdateStreamRequest & { name: string }) => {
      await apiClient.put(apiEndpoints.streamDetail(environmentId, name), data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamsRoot(environmentId) });
    },
  });
}

export function useDeleteStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (name: string) => {
      await apiClient.delete(apiEndpoints.streamDetail(environmentId, name), {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamsRoot(environmentId) });
    },
  });
}

export function usePurgeStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (name: string) => {
      await apiClient.post(`${apiEndpoints.streamDetail(environmentId, name)}/purge`, null, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamsRoot(environmentId) });
    },
  });
}

export function useCreateConsumer(environmentId: string | null, streamName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateConsumerRequest) => {
      await apiClient.post(apiEndpoints.consumerList(environmentId, streamName), data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.consumersRoot(environmentId, streamName) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamDetail(environmentId, streamName) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamsRoot(environmentId) });
    },
  });
}

export function useDeleteConsumer(environmentId: string | null, streamName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (consumerName: string) => {
      await apiClient.delete(apiEndpoints.consumerDetail(environmentId, streamName, consumerName), {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.consumersRoot(environmentId, streamName) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamDetail(environmentId, streamName) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.streamsRoot(environmentId) });
    },
  });
}

export function useStreamMessages(environmentId: string | null, streamName: string | undefined, startSequence?: number, count?: number) {
  return useQuery({
    queryKey: queryKeys.streamMessages(environmentId, streamName, startSequence, count),
    queryFn: async () => {
      const params: Record<string, number> = {};
      if (startSequence != null) params.startSequence = startSequence;
      if (count != null) params.count = count;
      const response = await apiClient.get(apiEndpoints.streamMessages(environmentId, streamName), { params });
      return response.data as StreamMessageItem[];
    },
    enabled: !!environmentId && !!streamName,
  });
}
