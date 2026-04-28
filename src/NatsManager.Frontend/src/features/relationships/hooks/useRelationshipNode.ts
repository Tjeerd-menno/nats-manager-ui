import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { ResourceNode } from '../types';

export function useRelationshipNode(environmentId: string | null, nodeId: string | null) {
  return useQuery<ResourceNode>({
    queryKey: ['relationship-node', environmentId, nodeId],
    queryFn: async () => {
      const response = await apiClient.get<ResourceNode>(
        `/environments/${environmentId}/relationships/nodes/${encodeURIComponent(nodeId!)}`
      );
      return response.data;
    },
    enabled: !!environmentId && !!nodeId,
    staleTime: 30_000,
  });
}
