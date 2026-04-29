import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { pollingIntervals } from '../../../api/queryConfig';
import { queryKeys } from '../../../api/queryKeys';
import type { DashboardSummary } from '../types';

export function useDashboard(environmentId: string | null) {
  return useQuery({
    queryKey: queryKeys.dashboard(environmentId),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.dashboard(environmentId));
      return response.data as DashboardSummary;
    },
    enabled: !!environmentId,
    refetchInterval: pollingIntervals.dashboard,
  });
}
