import { Button, Card, Checkbox, Group, MultiSelect, NumberInput, Select, Slider, Stack, Text } from '@mantine/core';
import type {
  MapFilter,
  RelationshipConfidence,
  RelationshipType,
  ResourceHealthStatus,
  ResourceType,
} from './types';

const resourceTypeOptions: ResourceType[] = [
  'Server',
  'Subject',
  'Stream',
  'Consumer',
  'KvBucket',
  'KvKey',
  'ObjectBucket',
  'Object',
  'ObjectStoreObject',
  'Service',
  'Endpoint',
  'ServiceEndpoint',
  'Alert',
  'Event',
  'External',
  'JetStreamAccount',
  'Client',
];

const relationshipTypeOptions: RelationshipType[] = [
  'Contains',
  'ConsumesFrom',
  'PublishesTo',
  'SubscribesTo',
  'UsesSubject',
  'BackedByStream',
  'HostedOn',
  'RoutedThrough',
  'HostsJetStream',
  'AffectedBy',
  'RelatedEvent',
  'DependsOn',
  'ExternalReference',
];

const healthStateOptions: ResourceHealthStatus[] = [
  'Healthy',
  'Warning',
  'Degraded',
  'Stale',
  'Unavailable',
  'Unknown',
];

const confidenceOptions: RelationshipConfidence[] = ['High', 'Medium', 'Low', 'Unknown'];

interface RelationshipFiltersProps {
  filters: MapFilter;
  onChange: (filters: MapFilter) => void;
  onClear: () => void;
}

function emptyToNull<T>(value: string[]): T[] | null {
  return value.length === 0 ? null : (value as T[]);
}

export function RelationshipFilters({ filters, onChange, onClear }: RelationshipFiltersProps) {
  return (
    <Card withBorder>
      <Stack gap="sm">
        <Group justify="space-between">
          <Text fw={600}>Filters</Text>
          <Button onClick={onClear} size="xs" variant="subtle">
            Clear
          </Button>
        </Group>

        <Stack gap={4}>
          <Text size="sm" fw={500}>Depth: {filters.depth}</Text>
          <Slider
            aria-label="Relationship depth"
            label={(value) => `${value} hop${value === 1 ? '' : 's'}`}
            marks={[
              { value: 1, label: '1' },
              { value: 2, label: '2' },
              { value: 3, label: '3' },
            ]}
            max={3}
            min={1}
            onChange={(depth) => onChange({ ...filters, depth })}
            value={filters.depth}
          />
        </Stack>

        <MultiSelect
          clearable
          data={resourceTypeOptions}
          label="Resource types"
          onChange={(value) => onChange({ ...filters, resourceTypes: emptyToNull<ResourceType>(value) })}
          placeholder="All resources"
          searchable
          value={filters.resourceTypes ?? []}
        />

        <MultiSelect
          clearable
          data={relationshipTypeOptions}
          label="Relationship types"
          onChange={(value) => onChange({ ...filters, relationshipTypes: emptyToNull<RelationshipType>(value) })}
          placeholder="All relationships"
          searchable
          value={filters.relationshipTypes ?? []}
        />

        <MultiSelect
          clearable
          data={healthStateOptions}
          label="Health states"
          onChange={(value) => onChange({ ...filters, healthStates: emptyToNull<ResourceHealthStatus>(value) })}
          placeholder="All health states"
          value={filters.healthStates ?? []}
        />

        <Select
          allowDeselect={false}
          data={confidenceOptions}
          label="Minimum confidence"
          onChange={(value) =>
            onChange({ ...filters, minimumConfidence: (value ?? 'Low') as RelationshipConfidence })
          }
          value={filters.minimumConfidence}
        />

        <Group grow>
          <Checkbox
            checked={filters.includeInferred}
            label="Include inferred"
            onChange={(event) => onChange({ ...filters, includeInferred: event.currentTarget.checked })}
          />
          <Checkbox
            checked={filters.includeStale}
            label="Include stale"
            onChange={(event) => onChange({ ...filters, includeStale: event.currentTarget.checked })}
          />
        </Group>

        <Group grow>
          <NumberInput
            clampBehavior="strict"
            label="Max nodes"
            max={500}
            min={1}
            onChange={(value) => onChange({ ...filters, maxNodes: Number(value) || 100 })}
            value={filters.maxNodes}
          />
          <NumberInput
            clampBehavior="strict"
            label="Max edges"
            max={2000}
            min={1}
            onChange={(value) => onChange({ ...filters, maxEdges: Number(value) || 500 })}
            value={filters.maxEdges}
          />
        </Group>
      </Stack>
    </Card>
  );
}
