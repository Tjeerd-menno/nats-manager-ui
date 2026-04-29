import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints } from '../../../api/endpoints';
import { queryKeys } from '../../../api/queryKeys';
import type { AuditEventsResult } from '../types';

export function useAuditEvents(params: {
  page?: number;
  pageSize?: number;
  actionType?: string;
  resourceType?: string;
  environmentId?: string;
}) {
  return useQuery({
    queryKey: queryKeys.auditEvents(params),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.auditEvents(), { params });
      return response.data as AuditEventsResult;
    },
  });
}
