import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { DashboardSummary } from '../types';

export function useDashboard(environmentId: string | null) {
  return useQuery({
    queryKey: ['dashboard', environmentId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/monitoring/dashboard`);
      return response.data as DashboardSummary;
    },
    enabled: !!environmentId,
    refetchInterval: 30000,
  });
}
