import { Center, Loader, Text, Stack } from '@mantine/core';

export function LoadingState({ message = 'Loading...' }: { message?: string }) {
  return (
    <Center h={300}>
      <Stack align="center">
        <Loader size="lg" />
        <Text c="dimmed">{message}</Text>
      </Stack>
    </Center>
  );
}
