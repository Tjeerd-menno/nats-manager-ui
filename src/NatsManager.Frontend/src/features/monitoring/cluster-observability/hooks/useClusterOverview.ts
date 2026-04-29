import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../../api/client';
import { apiEndpoints } from '../../../../api/endpoints';
import { pollingIntervals } from '../../../../api/queryConfig';
import { queryKeys } from '../../../../api/queryKeys';
import type { ClusterObservation } from '../types';

export function useClusterOverview(envId: string | null) {
  return useQuery({
    queryKey: queryKeys.clusterOverview(envId),
    queryFn: async () => {
      const response = await apiClient.get<ClusterObservation>(apiEndpoints.clusterOverview(envId));
      return response.data;
    },
    enabled: !!envId,
    refetchInterval: pollingIntervals.clusterOverview,
  });
}
