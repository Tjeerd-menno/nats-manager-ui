import { Center, Stack, ThemeIcon, Text, Button } from '@mantine/core';
import { IconPlugOff } from '@tabler/icons-react';

interface ClusterUnavailableStateProps {
  message?: string;
  onRetry?: () => void;
}

export function ClusterUnavailableState({
  message = 'Cluster observability data is currently unavailable.',
  onRetry,
}: ClusterUnavailableStateProps) {
  return (
    <Center h={300}>
      <Stack align="center" gap="sm">
        <ThemeIcon variant="light" color="red" size={56} radius="xl">
          <IconPlugOff size={28} />
        </ThemeIcon>
        <Text c="dimmed" size="sm" ta="center" maw={360}>{message}</Text>
        {onRetry && (
          <Button variant="light" size="sm" onClick={onRetry}>
            Retry
          </Button>
        )}
      </Stack>
    </Center>
  );
}
