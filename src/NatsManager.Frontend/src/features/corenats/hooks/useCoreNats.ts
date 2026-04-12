import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { NatsServerInfo } from '../types';

export function useCoreNatsStatus(environmentId: string | null) {
  return useQuery({
    queryKey: ['core-nats-status', environmentId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/core-nats/status`);
      return response.data as NatsServerInfo;
    },
    enabled: !!environmentId,
    refetchInterval: 15000,
  });
}

export function usePublishMessage(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ subject, payload }: { subject: string; payload?: string }) => {
      await apiClient.post(`/environments/${environmentId}/core-nats/publish`, {
        subject,
        payload,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['core-nats-status', environmentId] });
    },
  });
}
