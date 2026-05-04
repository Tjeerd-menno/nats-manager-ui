import { Modal, TextInput, NumberInput, Stack, Button, Group } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useCreateKvBucket } from '../hooks/useKv';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { validateInteger, validateNatsName, validateNonNegativeInteger, validateUnlimitedInteger } from '../../../shared/validation';

interface KvBucketFormProps {
  opened: boolean;
  onClose: () => void;
}

export function KvBucketForm({ opened, onClose }: KvBucketFormProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const createBucket = useCreateKvBucket(selectedEnvironmentId);

  const form = useForm({
    initialValues: {
      bucketName: '',
      history: 1,
      maxBytes: -1,
      maxValueSize: -1,
      ttl: 0,
    },
    validate: {
      bucketName: (v) => validateNatsName(v, 'Bucket name'),
      history: (v) => validateInteger(v, 'History', 1, 64),
      maxBytes: (v) => validateUnlimitedInteger(v, 'Max Bytes', 1),
      maxValueSize: (v) => validateUnlimitedInteger(v, 'Max Value Size', 1),
      ttl: (v) => validateNonNegativeInteger(v, 'TTL'),
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    createBucket.mutate(
      {
        bucketName: values.bucketName,
        history: values.history,
        maxBytes: values.maxBytes > 0 ? values.maxBytes : undefined,
        maxValueSize: values.maxValueSize > 0 ? values.maxValueSize : undefined,
        ttl: values.ttl > 0 ? values.ttl : undefined,
      },
      {
        onSuccess: () => {
          form.reset();
          onClose();
        },
      },
    );
  });

  return (
    <Modal opened={opened} onClose={onClose} title="Create KV Bucket">
      <form onSubmit={handleSubmit}>
        <Stack>
          <TextInput label="Bucket Name" required description="Letters, numbers, dots, hyphens, and underscores only" {...form.getInputProps('bucketName')} />
          <NumberInput label="History" description="Max historical entries per key" min={1} max={64} {...form.getInputProps('history')} />
          <NumberInput label="Max Bytes" description="Max bucket size (-1 for unlimited)" {...form.getInputProps('maxBytes')} />
          <NumberInput label="Max Value Size" description="Max value size per key (-1 for unlimited)" {...form.getInputProps('maxValueSize')} />
          <NumberInput label="TTL (seconds)" description="Max age for entries (0 for no TTL)" min={0} {...form.getInputProps('ttl')} />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>Cancel</Button>
            <Button type="submit" loading={createBucket.isPending}>Create</Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
