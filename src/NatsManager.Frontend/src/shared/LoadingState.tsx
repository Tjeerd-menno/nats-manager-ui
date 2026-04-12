import { Center, Loader, Text, Stack } from '@mantine/core';

export function LoadingState({ message = 'Loading…' }: { message?: string }) {
  return (
    <Center h={300}>
      <Stack align="center" gap="sm">
        <Loader size="lg" type="dots" />
        <Text c="dimmed" size="sm">{message}</Text>
      </Stack>
    </Center>
  );
}
