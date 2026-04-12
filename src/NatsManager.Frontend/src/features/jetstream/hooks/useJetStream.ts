import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { PaginatedResult, PaginatedQueryParams } from '../../../api/types';
import type { StreamListItem, StreamDetail, ConsumerInfo, CreateStreamRequest, UpdateStreamRequest, CreateConsumerRequest, StreamMessageItem } from '../types';

export function useStreams(environmentId: string | null, params?: PaginatedQueryParams) {
  return useQuery({
    queryKey: ['streams', environmentId, params],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/jetstream/streams`, { params });
      return response.data as PaginatedResult<StreamListItem>;
    },
    enabled: !!environmentId,
  });
}

export function useStream(environmentId: string | null, streamName: string | undefined) {
  return useQuery({
    queryKey: ['streams', environmentId, streamName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/jetstream/streams/${streamName}`);
      return response.data as StreamDetail;
    },
    enabled: !!environmentId && !!streamName,
  });
}

export function useConsumers(environmentId: string | null, streamName: string | undefined) {
  return useQuery({
    queryKey: ['consumers', environmentId, streamName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/jetstream/streams/${streamName}/consumers`);
      return response.data as ConsumerInfo[];
    },
    enabled: !!environmentId && !!streamName,
  });
}

export function useConsumer(environmentId: string | null, streamName: string | undefined, consumerName: string | undefined) {
  return useQuery({
    queryKey: ['consumers', environmentId, streamName, consumerName],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/jetstream/streams/${streamName}/consumers/${consumerName}`);
      return response.data as ConsumerInfo;
    },
    enabled: !!environmentId && !!streamName && !!consumerName,
  });
}

// Mutation hooks

export function useCreateStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateStreamRequest) => {
      await apiClient.post(`/environments/${environmentId}/jetstream/streams`, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['streams', environmentId] });
    },
  });
}

export function useUpdateStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ name, ...data }: UpdateStreamRequest & { name: string }) => {
      await apiClient.put(`/environments/${environmentId}/jetstream/streams/${name}`, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['streams', environmentId] });
    },
  });
}

export function useDeleteStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (name: string) => {
      await apiClient.delete(`/environments/${environmentId}/jetstream/streams/${name}`, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['streams', environmentId] });
    },
  });
}

export function usePurgeStream(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (name: string) => {
      await apiClient.post(`/environments/${environmentId}/jetstream/streams/${name}/purge`, null, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['streams', environmentId] });
    },
  });
}

export function useCreateConsumer(environmentId: string | null, streamName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateConsumerRequest) => {
      await apiClient.post(`/environments/${environmentId}/jetstream/streams/${streamName}/consumers`, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['consumers', environmentId, streamName] });
      void queryClient.invalidateQueries({ queryKey: ['streams', environmentId, streamName] });
    },
  });
}

export function useDeleteConsumer(environmentId: string | null, streamName: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (consumerName: string) => {
      await apiClient.delete(`/environments/${environmentId}/jetstream/streams/${streamName}/consumers/${consumerName}`, {
        headers: { 'X-Confirm': 'true' },
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['consumers', environmentId, streamName] });
      void queryClient.invalidateQueries({ queryKey: ['streams', environmentId, streamName] });
    },
  });
}

export function useStreamMessages(environmentId: string | null, streamName: string | undefined, startSequence?: number, count?: number) {
  return useQuery({
    queryKey: ['stream-messages', environmentId, streamName, startSequence, count],
    queryFn: async () => {
      const params: Record<string, number> = {};
      if (startSequence != null) params.startSequence = startSequence;
      if (count != null) params.count = count;
      const response = await apiClient.get(`/environments/${environmentId}/jetstream/streams/${streamName}/messages`, { params });
      return response.data as StreamMessageItem[];
    },
    enabled: !!environmentId && !!streamName,
  });
}
