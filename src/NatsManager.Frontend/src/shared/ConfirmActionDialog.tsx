import { Modal, Text, TextInput, Group, Button } from '@mantine/core';
import { useState } from 'react';

interface ConfirmActionDialogProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  resourceName: string;
  confirmLabel?: string;
  isLoading?: boolean;
}

export function ConfirmActionDialog({
  opened,
  onClose,
  onConfirm,
  title,
  message,
  resourceName,
  confirmLabel = 'Delete',
  isLoading = false,
}: ConfirmActionDialogProps) {
  const [confirmation, setConfirmation] = useState('');
  const isConfirmed = confirmation === resourceName;

  const handleClose = () => {
    setConfirmation('');
    onClose();
  };

  return (
    <Modal opened={opened} onClose={handleClose} title={title}>
      <Text mb="md">{message}</Text>
      <Text size="sm" c="dimmed" mb="xs">
        Type <strong>{resourceName}</strong> to confirm:
      </Text>
      <TextInput
        value={confirmation}
        onChange={(e) => setConfirmation(e.currentTarget.value)}
        placeholder={resourceName}
        mb="lg"
      />
      <Group justify="flex-end">
        <Button variant="default" onClick={handleClose}>Cancel</Button>
        <Button
          color="red"
          onClick={onConfirm}
          disabled={!isConfirmed}
          loading={isLoading}
        >
          {confirmLabel}
        </Button>
      </Group>
    </Modal>
  );
}
