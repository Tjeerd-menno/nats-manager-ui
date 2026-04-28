import { Stack, Text, Group, Badge, Title, Divider, ScrollArea, Anchor } from '@mantine/core';
import type { ResourceNode, RelationshipEdge, RelationshipFreshness } from './types';

const freshnessColor: Record<RelationshipFreshness, string> = {
  Live: 'green',
  Stale: 'gray',
  Partial: 'yellow',
  Unavailable: 'red',
};

interface RelationshipEvidencePanelProps {
  selectedNode: ResourceNode | null;
  selectedEdge?: RelationshipEdge | null;
  onOpenDetails?: (node: ResourceNode) => void;
}

const statusColor: Record<string, string> = {
  Healthy: 'green',
  Warning: 'yellow',
  Degraded: 'orange',
  Stale: 'gray',
  Unavailable: 'red',
  Unknown: 'gray',
};

const confidenceColor: Record<string, string> = {
  High: 'green',
  Medium: 'yellow',
  Low: 'red',
  Unknown: 'gray',
};

export function RelationshipEvidencePanel({
  selectedNode,
  selectedEdge,
  onOpenDetails,
}: RelationshipEvidencePanelProps) {
  if (!selectedNode && !selectedEdge) {
    return (
      <Stack p="md">
        <Text c="dimmed" size="sm">
          Click a node or edge to view details.
        </Text>
      </Stack>
    );
  }

  if (selectedNode) {
    return (
      <ScrollArea h={520} p="md">
        <Stack gap="xs">
          <Group justify="space-between">
            <Title order={5}>{selectedNode.displayName}</Title>
            <Badge color={statusColor[selectedNode.status] ?? 'gray'}>
              {selectedNode.status}
            </Badge>
          </Group>
          <Text size="xs" c="dimmed">
            {selectedNode.resourceType} · {selectedNode.resourceId}
          </Text>
          <Badge color={freshnessColor[selectedNode.freshness]} variant="dot">
            {selectedNode.freshness}
          </Badge>
          {selectedNode.isFocal && <Badge color="blue">Focal</Badge>}
          {selectedNode.detailRoute && onOpenDetails && (
            <Anchor
              component="button"
              size="sm"
              onClick={() => onOpenDetails(selectedNode)}
            >
              Open detail page →
            </Anchor>
          )}
          {Object.keys(selectedNode.metadata).length > 0 && (
            <>
              <Divider label="Metadata" labelPosition="left" mt="xs" />
              {Object.entries(selectedNode.metadata).map(([k, v]) => (
                <Group key={k} gap="xs">
                  <Text size="xs" fw={600} c="dimmed">
                    {k}:
                  </Text>
                  <Text size="xs">{v}</Text>
                </Group>
              ))}
            </>
          )}
        </Stack>
      </ScrollArea>
    );
  }

  if (selectedEdge) {
    return (
      <ScrollArea h={520} p="md">
        <Stack gap="xs">
          <Group justify="space-between">
            <Title order={5}>{selectedEdge.relationshipType}</Title>
            <Badge color={confidenceColor[selectedEdge.confidence] ?? 'gray'}>
              {selectedEdge.confidence}
            </Badge>
          </Group>
          <Text size="xs" c="dimmed">
            {selectedEdge.direction} · {selectedEdge.observationKind}
          </Text>
          <Badge color={freshnessColor[selectedEdge.freshness]} variant="dot">
            {selectedEdge.freshness}
          </Badge>
          <Divider label={`Evidence (${selectedEdge.evidence.length})`} labelPosition="left" mt="xs" />
          {selectedEdge.evidence.map((ev, i) => (
            <Stack key={i} gap={2}>
              <Group gap="xs">
                <Badge size="xs" color={ev.observedAt ? 'blue' : 'gray'}>
                  {ev.sourceModule}
                </Badge>
                <Text size="xs">{ev.evidenceType}</Text>
              </Group>
              <Text size="xs" c="dimmed">
                {ev.summary}
              </Text>
            </Stack>
          ))}
        </Stack>
      </ScrollArea>
    );
  }

  return null;
}
