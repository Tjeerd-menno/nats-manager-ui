import { useMemo } from 'react';
import { useLiveQuery } from '@tanstack/react-db';
import { relationshipEdgesCollection, relationshipNodesCollection } from '../collections';
import type { MapFilter, RelationshipConfidence, RelationshipEdge, ResourceNode } from '../../features/relationships/types';

interface RelationshipMapProjectionOptions {
  environmentId: string | null;
  filters?: Partial<MapFilter>;
}

const confidenceRank: Record<RelationshipConfidence, number> = {
  Unknown: 0,
  Low: 1,
  Medium: 2,
  High: 3,
};

export function useRelationshipMapProjection({ environmentId, filters = {} }: RelationshipMapProjectionOptions) {
  const nodeQuery = useLiveQuery((q) => q.from({ node: relationshipNodesCollection }), []);
  const edgeQuery = useLiveQuery((q) => q.from({ edge: relationshipEdgesCollection }), []);

  const data = useMemo(() => {
    const minConfidence = filters.minimumConfidence ?? 'Low';
    const maxNodes = filters.maxNodes ?? 100;
    const maxEdges = filters.maxEdges ?? 500;

    const nodes = (nodeQuery.data ?? [])
      .filter(node => !environmentId || node.environmentId === environmentId)
      .filter(node => !filters.resourceTypes?.length || filters.resourceTypes.includes(node.resourceType))
      .filter(node => !filters.healthStates?.length || filters.healthStates.includes(node.healthStatus))
      .slice(0, maxNodes);

    const nodeIds = new Set(nodes.map(node => node.id));
    const edges = (edgeQuery.data ?? [])
      .filter(edge => !environmentId || edge.environmentId === environmentId)
      .filter(edge => nodeIds.has(edge.sourceNodeId) && nodeIds.has(edge.targetNodeId))
      .filter(edge => !filters.relationshipTypes?.length || filters.relationshipTypes.includes(edge.relationshipType))
      .filter(edge => confidenceRank[edge.confidence] >= confidenceRank[minConfidence])
      .filter(edge => filters.includeInferred ?? true ? true : !edge.inferred)
      .filter(edge => filters.includeStale ?? true ? true : !edge.stale)
      .slice(0, maxEdges);

    return {
      nodes: nodes.map<ResourceNode>(node => ({
        nodeId: node.id,
        environmentId: node.environmentId,
        resourceType: node.resourceType,
        resourceId: node.resourceId,
        displayName: node.label,
        status: node.healthStatus,
        freshness: node.freshness,
        isFocal: false,
        detailRoute: node.detailRoute,
        metadata: node.metadata,
      })),
      edges: edges.map<RelationshipEdge>(edge => ({
        edgeId: edge.id,
        environmentId: edge.environmentId,
        sourceNodeId: edge.sourceNodeId,
        targetNodeId: edge.targetNodeId,
        relationshipType: edge.relationshipType,
        direction: 'Unknown',
        observationKind: edge.inferred ? 'Inferred' : 'Observed',
        confidence: edge.confidence,
        freshness: edge.freshness,
        status: edge.healthStatus,
        evidence: edge.evidence,
      })),
    };
  }, [edgeQuery.data, environmentId, filters.healthStates, filters.includeInferred, filters.includeStale, filters.maxEdges, filters.maxNodes, filters.minimumConfidence, filters.relationshipTypes, filters.resourceTypes, nodeQuery.data]);

  return {
    data,
    isLoading: nodeQuery.isLoading || edgeQuery.isLoading,
    isError: nodeQuery.isError || edgeQuery.isError,
  };
}
