import { Button } from '@mantine/core';
import { IconShare2 } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import type { ResourceType } from '../types';

interface OpenRelationshipMapButtonProps {
  environmentId: string;
  resourceType: ResourceType;
  resourceId: string;
  label?: string;
}

export function OpenRelationshipMapButton({
  environmentId,
  resourceType,
  resourceId,
  label = 'View Relationships',
}: OpenRelationshipMapButtonProps) {
  const navigate = useNavigate();

  const handleClick = () => {
    const params = new URLSearchParams({ resourceType, resourceId });
    navigate(`/environments/${environmentId}/relationships?${params.toString()}`);
  };

  return (
    <Button
      variant="light"
      size="sm"
      leftSection={<IconShare2 size={14} />}
      onClick={handleClick}
    >
      {label}
    </Button>
  );
}
