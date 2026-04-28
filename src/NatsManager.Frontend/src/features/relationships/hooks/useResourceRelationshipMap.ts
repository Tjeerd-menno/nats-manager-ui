import { useCallback, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate } from 'react-router-dom';
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
  const location = useLocation();
  const [selectedNode, setSelectedNode] = useState<ResourceNode | null>(null);

  const depth = filters.depth ?? 1;
  const maxNodes = filters.maxNodes ?? 100;
  const maxEdges = filters.maxEdges ?? 500;
  const minimumConfidence = filters.minimumConfidence ?? 'Low';
  const includeInferred = filters.includeInferred ?? true;
  const includeStale = filters.includeStale ?? true;

  const enabled = !!environmentId && !!resourceType && !!resourceId;
  const resourceTypes = filters.resourceTypes ?? null;
  const relationshipTypes = filters.relationshipTypes ?? null;
  const healthStates = filters.healthStates ?? null;

  const query = useQuery<RelationshipMap>({
    queryKey: [
      'relationship-map',
      environmentId,
      resourceType,
      resourceId,
      depth,
      maxNodes,
      maxEdges,
      minimumConfidence,
      includeInferred,
      includeStale,
      resourceTypes?.join(',') ?? '',
      relationshipTypes?.join(',') ?? '',
      healthStates?.join(',') ?? '',
    ],
    queryFn: async () => {
      const params = new URLSearchParams({
        resourceType: resourceType!,
        resourceId: resourceId!,
        depth: String(depth),
        maxNodes: String(maxNodes),
        maxEdges: String(maxEdges),
        minimumConfidence,
        includeInferred: String(includeInferred),
        includeStale: String(includeStale),
      });
      if (resourceTypes?.length) {
        params.set('resourceTypes', resourceTypes.join(','));
      }
      if (relationshipTypes?.length) {
        params.set('relationshipTypes', relationshipTypes.join(','));
      }
      if (healthStates?.length) {
        params.set('healthStates', healthStates.join(','));
      }
      const response = await apiClient.get<RelationshipMap>(
        `/environments/${environmentId}/relationships/map?${params.toString()}`
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
      const params = new URLSearchParams(location.search);
      params.set('resourceType', node.resourceType);
      params.set('resourceId', node.resourceId);
      params.delete('type');
      params.delete('id');
      navigate(`/environments/${environmentId}/relationships?${params.toString()}`);
    },
    [environmentId, location.search, navigate]
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
