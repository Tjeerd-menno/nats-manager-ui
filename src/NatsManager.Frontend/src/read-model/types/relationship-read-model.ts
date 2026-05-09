import type {
  RelationshipConfidence,
  RelationshipEvidence,
  RelationshipFreshness,
  RelationshipType,
  ResourceHealthStatus,
  ResourceType,
} from '../../features/relationships/types';

export interface RelationshipNodeRecord {
  id: string;
  environmentId: string;
  resourceType: ResourceType;
  resourceId: string;
  label: string;
  healthStatus: ResourceHealthStatus;
  freshness: RelationshipFreshness;
  detailRoute: string | null;
  lastObservedAtUtc: string | null;
  metadata: Record<string, string>;
}

export interface RelationshipEdgeRecord {
  id: string;
  environmentId: string;
  sourceNodeId: string;
  targetNodeId: string;
  relationshipType: RelationshipType;
  confidence: RelationshipConfidence;
  inferred: boolean;
  stale: boolean;
  healthStatus: ResourceHealthStatus;
  freshness: RelationshipFreshness;
  evidence: RelationshipEvidence[];
  lastObservedAtUtc: string | null;
}

export interface RelationshipEvidenceRecord {
  id: string;
  environmentId: string;
  edgeId: string;
  evidenceType: string;
  summary: string;
  observedAtUtc: string;
}
