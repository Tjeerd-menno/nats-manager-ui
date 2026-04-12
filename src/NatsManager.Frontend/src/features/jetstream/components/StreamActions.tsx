import { Button, Group } from '@mantine/core';
import { modals } from '@mantine/modals';
import { useDeleteStream, usePurgeStream } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';

interface StreamActionsProps {
  streamName: string;
  onDeleted?: () => void;
}

export function StreamActions({ streamName, onDeleted }: StreamActionsProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const deleteMutation = useDeleteStream(selectedEnvironmentId);
  const purgeMutation = usePurgeStream(selectedEnvironmentId);

  const handleDelete = () => {
    modals.openConfirmModal({
      title: 'Delete Stream',
      children: `Are you sure you want to delete stream "${streamName}"? This action cannot be undone.`,
      labels: { confirm: 'Delete', cancel: 'Cancel' },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        await deleteMutation.mutateAsync(streamName);
        onDeleted?.();
      },
    });
  };

  const handlePurge = () => {
    modals.openConfirmModal({
      title: 'Purge Stream',
      children: `Are you sure you want to purge all messages from stream "${streamName}"? This action cannot be undone.`,
      labels: { confirm: 'Purge', cancel: 'Cancel' },
      confirmProps: { color: 'orange' },
      onConfirm: async () => {
        await purgeMutation.mutateAsync(streamName);
      },
    });
  };

  return (
    <Group>
      <Button
        variant="outline"
        color="orange"
        size="xs"
        onClick={handlePurge}
        loading={purgeMutation.isPending}
      >
        Purge
      </Button>
      <Button
        variant="outline"
        color="red"
        size="xs"
        onClick={handleDelete}
        loading={deleteMutation.isPending}
      >
        Delete
      </Button>
    </Group>
  );
}
