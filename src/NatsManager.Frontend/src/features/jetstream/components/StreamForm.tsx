import { Modal, TextInput, Select, NumberInput, Button, Group, Stack, TagsInput } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useCreateStream, useUpdateStream } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { validateInteger, validateNatsName, validateNatsSubject, validateUnlimitedInteger } from '../../../shared/validation';
import type { StreamConfig } from '../types';

interface StreamFormProps {
  opened: boolean;
  onClose: () => void;
  existingConfig?: StreamConfig | null;
}

interface FormValues {
  name: string;
  description: string;
  subjects: string[];
  retentionPolicy: string;
  storageType: string;
  maxMessages: number;
  maxBytes: number;
  replicas: number;
  discardPolicy: string;
}

const retentionOptions = [
  { value: 'Limits', label: 'Limits' },
  { value: 'Interest', label: 'Interest' },
  { value: 'WorkQueue', label: 'Work Queue' },
];

const storageOptions = [
  { value: 'File', label: 'File' },
  { value: 'Memory', label: 'Memory' },
];

const discardOptions = [
  { value: 'Old', label: 'Old' },
  { value: 'New', label: 'New' },
];

export function StreamForm({ opened, onClose, existingConfig }: StreamFormProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const createMutation = useCreateStream(selectedEnvironmentId);
  const updateMutation = useUpdateStream(selectedEnvironmentId);
  const isEditing = !!existingConfig;

  const form = useForm<FormValues>({
    initialValues: {
      name: existingConfig?.name ?? '',
      description: existingConfig?.description ?? '',
      subjects: existingConfig?.subjects ?? [],
      retentionPolicy: existingConfig?.retentionPolicy ?? 'Limits',
      storageType: existingConfig?.storageType ?? 'File',
      maxMessages: existingConfig?.maxMessages ?? -1,
      maxBytes: existingConfig?.maxBytes ?? -1,
      replicas: existingConfig?.replicas ?? 1,
      discardPolicy: existingConfig?.discardPolicy ?? 'Old',
    },
    validate: {
      name: (value) => (isEditing ? null : validateNatsName(value, 'Stream name')),
      subjects: (value) => {
        if (value.length === 0) return 'At least one subject is required';
        for (const subject of value) {
          const error = validateNatsSubject(subject, 'Subject');
          if (error) return error;
        }
        return null;
      },
      maxMessages: (value) => validateUnlimitedInteger(value, 'Max Messages', 1),
      maxBytes: (value) => validateUnlimitedInteger(value, 'Max Bytes', 1),
      replicas: (value) => validateInteger(value, 'Replicas', 1, 5),
    },
  });

  const handleSubmit = form.onSubmit(async (values) => {
    if (isEditing) {
      await updateMutation.mutateAsync({
        name: existingConfig.name,
        description: values.description || undefined,
        subjects: values.subjects,
        maxMessages: values.maxMessages,
        maxBytes: values.maxBytes,
        replicas: values.replicas,
      });
    } else {
      await createMutation.mutateAsync({
        name: values.name,
        description: values.description || undefined,
        subjects: values.subjects,
        retentionPolicy: values.retentionPolicy,
        storageType: values.storageType,
        maxMessages: values.maxMessages,
        maxBytes: values.maxBytes,
        replicas: values.replicas,
        discardPolicy: values.discardPolicy,
      });
    }
    form.reset();
    onClose();
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? 'Update Stream' : 'Create Stream'}
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack>
          {!isEditing && (
            <TextInput
              label="Name"
              placeholder="my-stream"
              required
              description="Letters, numbers, dots, hyphens, and underscores only"
              {...form.getInputProps('name')}
            />
          )}
          <TextInput
            label="Description"
            placeholder="Optional description"
            {...form.getInputProps('description')}
          />
          <TagsInput
            label="Subjects"
            placeholder="Type a subject and press Enter"
            description="Use NATS subjects such as orders.*, payments.created, or user.>"
            {...form.getInputProps('subjects')}
          />
          {!isEditing && (
            <>
              <Select
                label="Retention Policy"
                data={retentionOptions}
                {...form.getInputProps('retentionPolicy')}
              />
              <Select
                label="Storage Type"
                data={storageOptions}
                {...form.getInputProps('storageType')}
              />
              <Select
                label="Discard Policy"
                data={discardOptions}
                {...form.getInputProps('discardPolicy')}
              />
            </>
          )}
          <NumberInput
            label="Max Messages"
            description="-1 for unlimited, or at least 1"
            {...form.getInputProps('maxMessages')}
          />
          <NumberInput
            label="Max Bytes"
            description="-1 for unlimited, or at least 1"
            {...form.getInputProps('maxBytes')}
          />
          <NumberInput
            label="Replicas"
            min={1}
            max={5}
            {...form.getInputProps('replicas')}
          />
          <Group justify="flex-end">
            <Button variant="subtle" onClick={onClose}>Cancel</Button>
            <Button
              type="submit"
              loading={createMutation.isPending || updateMutation.isPending}
            >
              {isEditing ? 'Update' : 'Create'}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
