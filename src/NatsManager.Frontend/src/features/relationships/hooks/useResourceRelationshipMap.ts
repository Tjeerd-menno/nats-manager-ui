import { useCallback, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../../api/client';
import type { RelationshipMap, ResourceType, MapFilter, ResourceNode } from '../types';

interface UseResourceRelationshipMapOptions {
  environmentId: string | null;
  resourceType: ResourceType | null;
  resourceId: string | null;
  filters?: Partial<MapFilter>;
}

export function useResourceRelationshipMap({
  environmentId,
  resourceType,
  resourceId,
  filters = {},
}: UseResourceRelationshipMapOptions) {
  const navigate = useNavigate();
  const [selectedNode, setSelectedNode] = useState<ResourceNode | null>(null);

  const depth = filters.depth ?? 2;
  const maxNodes = filters.maxNodes ?? 100;
  const maxEdges = filters.maxEdges ?? 500;
  const minConfidence = filters.minimumConfidence ?? 'Low';

  const enabled = !!environmentId && !!resourceType && !!resourceId;

  const query = useQuery<RelationshipMap>({
    queryKey: [
      'relationship-map',
      environmentId,
      resourceType,
      resourceId,
      depth,
      maxNodes,
      maxEdges,
      minConfidence,
      filters.resourceTypes,
      filters.relationshipTypes,
    ],
    queryFn: async () => {
      const params = new URLSearchParams({
        type: resourceType!,
        id: resourceId!,
        depth: String(depth),
        maxNodes: String(maxNodes),
        maxEdges: String(maxEdges),
        minConfidence,
      });
      if (filters.resourceTypes?.length) {
        params.set('resourceTypes', filters.resourceTypes.join(','));
      }
      if (filters.relationshipTypes?.length) {
        params.set('relationshipTypes', filters.relationshipTypes.join(','));
      }
      const response = await apiClient.get<RelationshipMap>(
        `/environments/${environmentId}/relationships?${params.toString()}`
      );
      return response.data;
    },
    enabled,
    placeholderData: (prev) => prev,
    staleTime: 30_000,
  });

  const recenter = useCallback(
    (node: ResourceNode) => {
      setSelectedNode(null);
      const params = new URLSearchParams({ type: node.resourceType, id: node.resourceId });
      navigate(`/environments/${environmentId}/relationships?${params.toString()}`);
    },
    [environmentId, navigate]
  );

  const openDetails = useCallback(
    (node: ResourceNode) => {
      if (node.detailRoute) {
        navigate(node.detailRoute);
      }
    },
    [navigate]
  );

  return {
    ...query,
    selectedNode,
    setSelectedNode,
    recenter,
    openDetails,
  };
}
