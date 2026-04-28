import { useCallback, useMemo } from 'react';
import { useSearchParams, useParams } from 'react-router-dom';
import { Stack, Title, Grid, Alert, Text, Badge, Group } from '@mantine/core';
import { IconShare2, IconAlertTriangle } from '@tabler/icons-react';
import { useResourceRelationshipMap } from './hooks/useResourceRelationshipMap';
import { RelationshipFlow } from './RelationshipFlow';
import { RelationshipEvidencePanel } from './RelationshipEvidencePanel';
import { RelationshipFilters } from './RelationshipFilters';
import { CollapsedBranchCount } from './components/CollapsedBranchCount';
import { EmptyFilterState } from './components/EmptyFilterState';
import { LoadingState } from '../../shared/LoadingState';
import { EmptyState } from '../../shared/EmptyState';
import { StaleDataBanner } from '../../shared/StaleDataBanner';
import type {
  MapFilter,
  RelationshipConfidence,
  RelationshipFreshness,
  RelationshipType,
  ResourceHealthStatus,
  ResourceType,
  RelationshipMap,
} from './types';

const defaultFilters: MapFilter = {
  depth: 1,
  resourceTypes: null,
  relationshipTypes: null,
  healthStates: null,
  minimumConfidence: 'Low',
  includeInferred: true,
  includeStale: true,
  maxNodes: 100,
  maxEdges: 500,
};

function splitParam<T extends string>(value: string | null): T[] | null {
  if (!value) return null;
  const values = value.split(',').map((item) => item.trim()).filter(Boolean);
  return values.length > 0 ? (values as T[]) : null;
}

function numberParam(value: string | null, fallback: number): number {
  const parsed = value ? Number(value) : Number.NaN;
  return Number.isFinite(parsed) ? parsed : fallback;
}

function boolParam(value: string | null, fallback: boolean): boolean {
  if (value === null) return fallback;
  return value.toLowerCase() !== 'false';
}

function getMapConnectionStatus(map: RelationshipMap): 'Available' | 'Degraded' | 'Unavailable' {
  const freshnessValues: RelationshipFreshness[] = [
    ...map.nodes.map((node) => node.freshness),
    ...map.edges.map((edge) => edge.freshness),
    ...map.edges.flatMap((edge) => edge.evidence.map((evidence) => evidence.freshness)),
  ];

  if (freshnessValues.includes('Unavailable')) return 'Unavailable';
  if (
    freshnessValues.includes('Partial') ||
    freshnessValues.includes('Stale') ||
    map.omittedCounts.unsafeRelationships > 0
  ) {
    return 'Degraded';
  }

  return 'Available';
}

export default function ResourceRelationshipMapPage() {
  const { envId } = useParams<{ envId: string }>();
  const [searchParams, setSearchParams] = useSearchParams();

  const typeParam = searchParams.get('resourceType') ?? searchParams.get('type');
  const idParam = searchParams.get('resourceId') ?? searchParams.get('id');

  const resourceType = typeParam as ResourceType | null;
  const resourceId = idParam;
  const filters = useMemo<MapFilter>(() => ({
    depth: Math.min(3, Math.max(1, numberParam(searchParams.get('depth'), defaultFilters.depth))),
    resourceTypes: splitParam<ResourceType>(searchParams.get('resourceTypes')),
    relationshipTypes: splitParam<RelationshipType>(searchParams.get('relationshipTypes')),
    healthStates: splitParam<ResourceHealthStatus>(searchParams.get('healthStates')),
    minimumConfidence: (searchParams.get('minimumConfidence') ??
      searchParams.get('minConfidence') ??
      defaultFilters.minimumConfidence) as RelationshipConfidence,
    includeInferred: boolParam(searchParams.get('includeInferred'), defaultFilters.includeInferred),
    includeStale: boolParam(searchParams.get('includeStale'), defaultFilters.includeStale),
    maxNodes: Math.min(500, Math.max(1, numberParam(searchParams.get('maxNodes'), defaultFilters.maxNodes))),
    maxEdges: Math.min(2000, Math.max(1, numberParam(searchParams.get('maxEdges'), defaultFilters.maxEdges))),
  }), [searchParams]);

  const updateFilters = useCallback((nextFilters: MapFilter) => {
    const next = new URLSearchParams(searchParams);
    if (resourceType) next.set('resourceType', resourceType);
    if (resourceId) next.set('resourceId', resourceId);
    next.delete('type');
    next.delete('id');
    next.delete('minConfidence');
    next.set('depth', String(nextFilters.depth));
    next.set('minimumConfidence', nextFilters.minimumConfidence);
    next.set('includeInferred', String(nextFilters.includeInferred));
    next.set('includeStale', String(nextFilters.includeStale));
    next.set('maxNodes', String(nextFilters.maxNodes));
    next.set('maxEdges', String(nextFilters.maxEdges));

    const setList = (key: string, values: readonly string[] | null) => {
      if (values?.length) next.set(key, values.join(','));
      else next.delete(key);
    };

    setList('resourceTypes', nextFilters.resourceTypes);
    setList('relationshipTypes', nextFilters.relationshipTypes);
    setList('healthStates', nextFilters.healthStates);
    setSearchParams(next, { replace: true });
  }, [resourceId, resourceType, searchParams, setSearchParams]);

  const clearFilters = useCallback(() => {
    updateFilters(defaultFilters);
  }, [updateFilters]);

  const { data, isLoading, isError, error, selectedNode, setSelectedNode, recenter, openDetails } =
    useResourceRelationshipMap({
      environmentId: envId ?? null,
      resourceType,
      resourceId,
      filters,
    });

  if (!envId || !resourceType || !resourceId) {
    return (
      <Stack>
        <Title order={2}>
          <Group gap="xs">
            <IconShare2 size={22} />
            Relationship Map
          </Group>
        </Title>
        <EmptyState
          message="Select a resource to view its relationship map. Use the 'View Relationships' button on any resource detail page."
          icon={IconShare2}
        />
      </Stack>
    );
  }

  if (isLoading) {
    return <LoadingState message="Building relationship map…" />;
  }

  if (isError) {
    const message = error instanceof Error ? error.message : 'Failed to load relationship map.';
    return (
      <Stack>
        <Title order={2}>Relationship Map</Title>
        <Alert color="red" icon={<IconAlertTriangle size={16} />} title="Error">
          {message}
        </Alert>
      </Stack>
    );
  }

  if (!data) {
    return (
      <Stack>
        <Title order={2}>Relationship Map</Title>
        <EmptyState message="Resource not found." icon={IconShare2} />
      </Stack>
    );
  }

  const omitted = data.omittedCounts;
  const hasOmissions =
    omitted.filteredNodes + omitted.filteredEdges + omitted.collapsedNodes + omitted.collapsedEdges > 0;

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>
          <Group gap="xs">
            <IconShare2 size={22} />
            Relationship Map
          </Group>
        </Title>
        <Group gap="xs">
          <Badge color="blue">{data.focalResource.resourceType}</Badge>
          <Text fw={600}>{data.focalResource.displayName}</Text>
          <Text size="xs" c="dimmed">
            {data.nodes.length} node(s) · {data.edges.length} edge(s)
          </Text>
        </Group>
      </Group>

      <StaleDataBanner
        connectionStatus={getMapConnectionStatus(data)}
        lastUpdated={data.generatedAt}
      />

      {hasOmissions && (
        <Alert color="yellow" icon={<IconAlertTriangle size={14} />} title="Some relationships omitted">
          {omitted.filteredNodes > 0 && `${omitted.filteredNodes} node(s) filtered. `}
          {omitted.filteredEdges > 0 && `${omitted.filteredEdges} edge(s) filtered. `}
          {omitted.collapsedNodes > 0 && `${omitted.collapsedNodes} node(s) collapsed. `}
          {omitted.unsafeRelationships > 0 &&
            `${omitted.unsafeRelationships} unsafe relationship(s) excluded.`}
        </Alert>
      )}

      {data.nodes.length === 0 && <EmptyFilterState onClearFilters={clearFilters} />}

      <Grid>
        <Grid.Col span={{ base: 12, md: 8 }}>
          {data.nodes.length > 0 && (
            <RelationshipFlow
              map={data}
              selectedNode={selectedNode}
              onNodeSelect={setSelectedNode}
              onRecenter={recenter}
            />
          )}
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 4 }}>
          <Stack>
            <RelationshipFilters
              filters={filters}
              onChange={updateFilters}
              onClear={clearFilters}
            />
            <CollapsedBranchCount
              omittedCounts={data.omittedCounts}
              maxNodes={filters.maxNodes}
              maxEdges={filters.maxEdges}
              onIncreaseLimits={(limits) => updateFilters({ ...filters, ...limits })}
            />
            <RelationshipEvidencePanel
              selectedNode={selectedNode}
              onOpenDetails={openDetails}
            />
          </Stack>
        </Grid.Col>
      </Grid>
    </Stack>
  );
}
