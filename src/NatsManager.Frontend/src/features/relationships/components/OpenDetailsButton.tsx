import { Button } from '@mantine/core';
import { IconExternalLink } from '@tabler/icons-react';
import type { ResourceNode } from '../types';

interface OpenDetailsButtonProps {
  node: ResourceNode;
  onOpenDetails: (node: ResourceNode) => void;
}

export function OpenDetailsButton({ node, onOpenDetails }: OpenDetailsButtonProps) {
  return (
    <Button
      disabled={!node.detailRoute}
      leftSection={<IconExternalLink size={14} />}
      onClick={() => onOpenDetails(node)}
      size="xs"
      variant="light"
    >
      {node.detailRoute ? 'Open details' : 'No details page'}
    </Button>
  );
}
