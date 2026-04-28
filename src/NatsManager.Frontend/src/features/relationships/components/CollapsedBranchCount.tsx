import { Alert, Button, Group, Text } from '@mantine/core';
import { IconGitBranch } from '@tabler/icons-react';
import type { OmittedCounts } from '../types';

interface CollapsedBranchCountProps {
  omittedCounts: OmittedCounts;
  maxNodes: number;
  maxEdges: number;
  onIncreaseLimits: (next: { maxNodes: number; maxEdges: number }) => void;
}

export function CollapsedBranchCount({
  omittedCounts,
  maxNodes,
  maxEdges,
  onIncreaseLimits,
}: CollapsedBranchCountProps) {
  const collapsedTotal = omittedCounts.collapsedNodes + omittedCounts.collapsedEdges;
  if (collapsedTotal === 0) {
    return null;
  }

  const nextMaxNodes = Math.min(500, Math.max(maxNodes + 25, Math.ceil(maxNodes * 1.5)));
  const nextMaxEdges = Math.min(2000, Math.max(maxEdges + 100, Math.ceil(maxEdges * 1.5)));
  const canShowMore = nextMaxNodes > maxNodes || nextMaxEdges > maxEdges;

  return (
    <Alert color="blue" icon={<IconGitBranch size={16} />} title="Branches collapsed">
      <Group justify="space-between" align="center">
        <Text size="sm">
          {omittedCounts.collapsedNodes} node(s) and {omittedCounts.collapsedEdges} edge(s) were collapsed by the current bounds.
        </Text>
        <Button
          disabled={!canShowMore}
          onClick={() => onIncreaseLimits({ maxNodes: nextMaxNodes, maxEdges: nextMaxEdges })}
          size="xs"
          variant="light"
        >
          Show more
        </Button>
      </Group>
    </Alert>
  );
}
