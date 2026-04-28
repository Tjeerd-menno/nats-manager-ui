import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../../api/client';
import type { ClusterObservation } from '../types';

export function useClusterOverview(envId: string | null) {
  return useQuery({
    queryKey: ['cluster-overview', envId],
    queryFn: async () => {
      const response = await apiClient.get<ClusterObservation>(
        `/environments/${envId}/monitoring/cluster/overview`
      );
      return response.data;
    },
    enabled: !!envId,
    refetchInterval: 30_000,
  });
}
