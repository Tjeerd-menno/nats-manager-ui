import { Badge } from '@mantine/core';
import { IconBellRinging } from '@tabler/icons-react';
import type { ResourceType } from '../types';

interface AlertHighlightProps {
  resourceType: ResourceType;
}

export function AlertHighlight({ resourceType }: AlertHighlightProps) {
  if (resourceType !== 'Alert' && resourceType !== 'Event') {
    return null;
  }

  return (
    <Badge
      aria-label={`${resourceType} relationship node`}
      color="red"
      leftSection={<IconBellRinging size={10} />}
      size="xs"
      variant="filled"
    >
      {resourceType}
    </Badge>
  );
}
