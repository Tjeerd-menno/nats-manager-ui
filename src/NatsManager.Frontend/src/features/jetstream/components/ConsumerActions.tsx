import { Button } from '@mantine/core';
import { modals } from '@mantine/modals';
import { useDeleteConsumer } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';

interface ConsumerActionsProps {
  streamName: string;
  consumerName: string;
  onDeleted?: () => void;
}

export function ConsumerActions({ streamName, consumerName, onDeleted }: ConsumerActionsProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const deleteMutation = useDeleteConsumer(selectedEnvironmentId, streamName);

  const handleDelete = () => {
    modals.openConfirmModal({
      title: 'Delete Consumer',
      children: `Are you sure you want to delete consumer "${consumerName}" from stream "${streamName}"?`,
      labels: { confirm: 'Delete', cancel: 'Cancel' },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        await deleteMutation.mutateAsync(consumerName);
        onDeleted?.();
      },
    });
  };

  return (
    <Button
      variant="outline"
      color="red"
      size="xs"
      onClick={handleDelete}
      loading={deleteMutation.isPending}
    >
      Delete
    </Button>
  );
}
