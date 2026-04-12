import type { ReactNode } from 'react';
import { Center, Text, Stack } from '@mantine/core';

interface EmptyStateProps {
  message?: string;
  action?: ReactNode;
}

export function EmptyState({ message = 'No items found', action }: EmptyStateProps) {
  return (
    <Center h={200}>
      <Stack align="center">
        <Text size="xl">📭</Text>
        <Text c="dimmed">{message}</Text>
        {action}
      </Stack>
    </Center>
  );
}
