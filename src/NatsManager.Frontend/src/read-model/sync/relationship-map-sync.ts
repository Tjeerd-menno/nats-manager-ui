import { relationshipEdgesCollection, relationshipEvidenceCollection, relationshipNodesCollection, resourceIndexCollection } from '../collections';
import type { RelationshipMap, RelationshipEvidence, ResourceHealthStatus } from '../../features/relationships/types';
import type { HealthStatus } from '../types/common';
import { makeEnvironmentScopedId, upsertRecord } from '../utils/collection-utils';

export function syncRelationshipMap(map: RelationshipMap) {
  for (const node of map.nodes) {
    upsertRecord(relationshipNodesCollection, node.nodeId, {
      id: node.nodeId,
      environmentId: node.environmentId,
      resourceType: node.resourceType,
      resourceId: node.resourceId,
      label: node.displayName,
      healthStatus: node.status,
      freshness: node.freshness,
      detailRoute: node.detailRoute,
      lastObservedAtUtc: map.generatedAt,
      metadata: node.metadata,
    });

    upsertRecord(resourceIndexCollection, makeEnvironmentScopedId(node.environmentId, node.resourceType, node.resourceId), {
      id: makeEnvironmentScopedId(node.environmentId, node.resourceType, node.resourceId),
      environmentId: node.environmentId,
      resourceType: node.resourceType === 'ObjectStoreObject' ? 'ObjectStoreObject' : node.resourceType,
      resourceId: node.resourceId,
      name: node.displayName,
      displayName: node.displayName,
      description: null,
      status: toHealthStatus(node.status),
      freshness: node.freshness === 'Live' ? 'Live' : node.freshness === 'Stale' ? 'Stale' : 'Unknown',
      detailRoute: node.detailRoute,
      searchableText: [node.displayName, node.resourceType, node.resourceId].join(' ').toLowerCase(),
      lastObservedAtUtc: map.generatedAt,
      indexedAtUtc: map.generatedAt,
    });
  }

  for (const edge of map.edges) {
    upsertRecord(relationshipEdgesCollection, edge.edgeId, {
      id: edge.edgeId,
      environmentId: edge.environmentId,
      sourceNodeId: edge.sourceNodeId,
      targetNodeId: edge.targetNodeId,
      relationshipType: edge.relationshipType,
      confidence: edge.confidence,
      inferred: edge.observationKind === 'Inferred',
      stale: edge.freshness === 'Stale',
      healthStatus: edge.status,
      freshness: edge.freshness,
      evidence: edge.evidence,
      lastObservedAtUtc: map.generatedAt,
    });

    edge.evidence.forEach((evidence, index) => {
      upsertRecord(relationshipEvidenceCollection, `${edge.edgeId}:${index}`, toEvidenceRecord(edge.environmentId, edge.edgeId, evidence, index));
    });
  }
}

function toEvidenceRecord(environmentId: string, edgeId: string, evidence: RelationshipEvidence, index: number) {
  return {
    id: `${edgeId}:${index}`,
    environmentId,
    edgeId,
    evidenceType: evidence.evidenceType,
    summary: evidence.summary,
    observedAtUtc: evidence.observedAt,
  };
}

function toHealthStatus(status: ResourceHealthStatus): HealthStatus {
  if (status === 'Healthy') return 'Ok';
  if (status === 'Warning' || status === 'Degraded' || status === 'Stale') return 'Degraded';
  if (status === 'Unavailable') return 'Unavailable';
  return 'Unknown';
}
