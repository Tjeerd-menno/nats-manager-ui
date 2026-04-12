import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';

interface AuditEventDto {
  id: string;
  timestamp: string;
  actorId: string | null;
  actorName: string;
  actionType: string;
  resourceType: string;
  resourceId: string;
  resourceName: string;
  environmentId: string | null;
  outcome: string;
  details: string | null;
  source: string;
}

interface AuditEventsResult {
  items: AuditEventDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export function useAuditEvents(params: {
  page?: number;
  pageSize?: number;
  actionType?: string;
  resourceType?: string;
  environmentId?: string;
}) {
  return useQuery({
    queryKey: ['audit-events', params],
    queryFn: async () => {
      const response = await apiClient.get('/audit/events', { params });
      return response.data as AuditEventsResult;
    },
  });
}
