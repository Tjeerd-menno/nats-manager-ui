import { Stack, Text, Button } from '@mantine/core';
import { IconFilterOff } from '@tabler/icons-react';
import { EmptyState } from '../../../shared/EmptyState';

interface EmptyFilterStateProps {
  onClearFilters: () => void;
}

export function EmptyFilterState({ onClearFilters }: EmptyFilterStateProps) {
  return (
    <Stack align="center" py="xl">
      <EmptyState
        icon={IconFilterOff}
        message="No relationships match the current filters. Clear filters or increase the graph bounds to reveal more relationships."
      />
      <Text c="dimmed" size="sm">
        The focal resource is still available; only the visible graph is filtered.
      </Text>
      <Button onClick={onClearFilters} variant="light">
        Clear filters
      </Button>
    </Stack>
  );
}
