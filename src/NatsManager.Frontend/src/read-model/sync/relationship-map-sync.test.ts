import { relationshipEdgesCollection, relationshipEvidenceCollection, relationshipNodesCollection } from '../collections';
import type { RelationshipMap } from '../../features/relationships/types';
import { syncRelationshipMap } from './relationship-map-sync';

describe('relationship map read-model sync', () => {
  beforeEach(() => {
    clearCollection(relationshipNodesCollection, record => record.id);
    clearCollection(relationshipEdgesCollection, record => record.id);
    clearCollection(relationshipEvidenceCollection, record => record.id);
  });

  it('stores nodes, edges, and evidence together', () => {
    syncRelationshipMap(createMap());

    expect(relationshipNodesCollection.get('stream-orders')?.label).toBe('ORDERS');
    expect(relationshipEdgesCollection.get('edge-1')?.sourceNodeId).toBe('consumer-worker');
    expect(relationshipEvidenceCollection.get('edge-1:0')?.summary).toBe('consumer reads stream');
  });

  it('removes stale evidence records when an edge is resynced with fewer evidence items', () => {
    const map = createMap();
    syncRelationshipMap({
      ...map,
      edges: [
        {
          ...map.edges[0],
          evidence: [
            ...map.edges[0].evidence,
            {
              sourceModule: 'Search',
              evidenceType: 'SearchResult',
              observedAt: '2026-01-01T00:00:01.000Z',
              freshness: 'Live',
              summary: 'search corroborates stream usage',
              safeFields: {},
            },
          ],
        },
      ],
    });

    syncRelationshipMap(map);

    expect(relationshipEvidenceCollection.get('edge-1:0')?.summary).toBe('consumer reads stream');
    expect(relationshipEvidenceCollection.get('edge-1:1')).toBeUndefined();
  });
});

function createMap(): RelationshipMap {
  return {
    environmentId: 'env-1',
    generatedAt: '2026-01-01T00:00:00.000Z',
    depth: 1,
    focalResource: {
      environmentId: 'env-1',
      resourceType: 'Stream',
      resourceId: 'ORDERS',
      displayName: 'ORDERS',
      route: null,
    },
    filters: {
      depth: 1,
      resourceTypes: null,
      relationshipTypes: null,
      healthStates: null,
      minimumConfidence: 'Low',
      includeInferred: true,
      includeStale: true,
      maxNodes: 100,
      maxEdges: 500,
    },
    omittedCounts: {
      filteredNodes: 0,
      filteredEdges: 0,
      collapsedNodes: 0,
      collapsedEdges: 0,
      unsafeRelationships: 0,
    },
    nodes: [
      {
        nodeId: 'stream-orders',
        environmentId: 'env-1',
        resourceType: 'Stream',
        resourceId: 'ORDERS',
        displayName: 'ORDERS',
        status: 'Healthy',
        freshness: 'Live',
        isFocal: true,
        detailRoute: null,
        metadata: {},
      },
      {
        nodeId: 'consumer-worker',
        environmentId: 'env-1',
        resourceType: 'Consumer',
        resourceId: 'worker',
        displayName: 'worker',
        status: 'Healthy',
        freshness: 'Live',
        isFocal: false,
        detailRoute: null,
        metadata: {},
      },
    ],
    edges: [
      {
        edgeId: 'edge-1',
        environmentId: 'env-1',
        sourceNodeId: 'consumer-worker',
        targetNodeId: 'stream-orders',
        relationshipType: 'ConsumesFrom',
        direction: 'Outbound',
        observationKind: 'Observed',
        confidence: 'High',
        freshness: 'Live',
        status: 'Healthy',
        evidence: [
          {
            sourceModule: 'JetStream',
            evidenceType: 'ConsumerInfo',
            observedAt: '2026-01-01T00:00:00.000Z',
            freshness: 'Live',
            summary: 'consumer reads stream',
            safeFields: {},
          },
        ],
      },
    ],
  };
}

function clearCollection<T extends object>(collection: { toArray: T[]; delete: (keys: string[]) => unknown }, getKey: (record: T) => string) {
  const ids = collection.toArray.map(getKey);
  if (ids.length > 0) collection.delete(ids);
}
