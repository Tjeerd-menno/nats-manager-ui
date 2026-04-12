import type { ReactNode } from 'react';
import { Center, Text, Stack, ThemeIcon } from '@mantine/core';
import { IconInbox } from '@tabler/icons-react';
import type { Icon } from '@tabler/icons-react';

interface EmptyStateProps {
  message?: string;
  action?: ReactNode;
  icon?: Icon;
}

export function EmptyState({ message = 'No items found', action, icon: IconComponent = IconInbox }: EmptyStateProps) {
  return (
    <Center h={200}>
      <Stack align="center" gap="sm">
        <ThemeIcon variant="light" color="gray" size={48} radius="xl">
          <IconComponent size={24} />
        </ThemeIcon>
        <Text c="dimmed" size="sm">{message}</Text>
        {action}
      </Stack>
    </Center>
  );
}
