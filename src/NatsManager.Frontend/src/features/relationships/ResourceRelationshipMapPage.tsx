import { useSearchParams, useParams } from 'react-router-dom';
import { Stack, Title, Grid, Alert, Text, Badge, Group } from '@mantine/core';
import { IconShare2, IconAlertTriangle } from '@tabler/icons-react';
import { useResourceRelationshipMap } from './hooks/useResourceRelationshipMap';
import { RelationshipFlow } from './RelationshipFlow';
import { RelationshipEvidencePanel } from './RelationshipEvidencePanel';
import { LoadingState } from '../../shared/LoadingState';
import { EmptyState } from '../../shared/EmptyState';
import type { ResourceType } from './types';

export default function ResourceRelationshipMapPage() {
  const { envId } = useParams<{ envId: string }>();
  const [searchParams] = useSearchParams();

  const typeParam = searchParams.get('type');
  const idParam = searchParams.get('id');

  const resourceType = typeParam as ResourceType | null;
  const resourceId = idParam;

  const { data, isLoading, isError, error, selectedNode, setSelectedNode, recenter, openDetails } =
    useResourceRelationshipMap({
      environmentId: envId ?? null,
      resourceType,
      resourceId,
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

      {hasOmissions && (
        <Alert color="yellow" icon={<IconAlertTriangle size={14} />} title="Some relationships omitted">
          {omitted.filteredNodes > 0 && `${omitted.filteredNodes} node(s) filtered. `}
          {omitted.filteredEdges > 0 && `${omitted.filteredEdges} edge(s) filtered. `}
          {omitted.collapsedNodes > 0 && `${omitted.collapsedNodes} node(s) collapsed. `}
          {omitted.unsafeRelationships > 0 &&
            `${omitted.unsafeRelationships} unsafe relationship(s) excluded.`}
        </Alert>
      )}

      <Grid>
        <Grid.Col span={{ base: 12, md: 8 }}>
          <RelationshipFlow
            map={data}
            selectedNode={selectedNode}
            onNodeSelect={setSelectedNode}
            onRecenter={recenter}
          />
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 4 }}>
          <RelationshipEvidencePanel
            selectedNode={selectedNode}
            onOpenDetails={openDetails}
          />
        </Grid.Col>
      </Grid>
    </Stack>
  );
}
