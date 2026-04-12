import { Modal, TextInput, Select, NumberInput, Button, Group, Stack } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useCreateConsumer } from '../hooks/useJetStream';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';

interface ConsumerFormProps {
  opened: boolean;
  onClose: () => void;
  streamName: string;
}

interface FormValues {
  name: string;
  description: string;
  deliverPolicy: string;
  ackPolicy: string;
  filterSubject: string;
  maxDeliver: number;
}

const deliverPolicyOptions = [
  { value: 'All', label: 'All' },
  { value: 'Last', label: 'Last' },
  { value: 'New', label: 'New' },
  { value: 'ByStartSequence', label: 'By Start Sequence' },
  { value: 'ByStartTime', label: 'By Start Time' },
  { value: 'LastPerSubject', label: 'Last Per Subject' },
];

const ackPolicyOptions = [
  { value: 'Explicit', label: 'Explicit' },
  { value: 'None', label: 'None' },
  { value: 'All', label: 'All' },
];

export function ConsumerForm({ opened, onClose, streamName }: ConsumerFormProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const createMutation = useCreateConsumer(selectedEnvironmentId, streamName);

  const form = useForm<FormValues>({
    initialValues: {
      name: '',
      description: '',
      deliverPolicy: 'All',
      ackPolicy: 'Explicit',
      filterSubject: '',
      maxDeliver: -1,
    },
    validate: {
      name: (value) => (value.trim().length === 0 ? 'Name is required' : null),
    },
  });

  const handleSubmit = form.onSubmit(async (values) => {
    await createMutation.mutateAsync({
      name: values.name,
      description: values.description || undefined,
      deliverPolicy: values.deliverPolicy,
      ackPolicy: values.ackPolicy,
      filterSubject: values.filterSubject || undefined,
      maxDeliver: values.maxDeliver,
    });
    form.reset();
    onClose();
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Create Consumer"
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack>
          <TextInput
            label="Name"
            placeholder="my-consumer"
            required
            {...form.getInputProps('name')}
          />
          <TextInput
            label="Description"
            placeholder="Optional description"
            {...form.getInputProps('description')}
          />
          <Select
            label="Deliver Policy"
            data={deliverPolicyOptions}
            {...form.getInputProps('deliverPolicy')}
          />
          <Select
            label="Ack Policy"
            data={ackPolicyOptions}
            {...form.getInputProps('ackPolicy')}
          />
          <TextInput
            label="Filter Subject"
            placeholder="Optional subject filter"
            {...form.getInputProps('filterSubject')}
          />
          <NumberInput
            label="Max Deliver"
            description="-1 for unlimited"
            {...form.getInputProps('maxDeliver')}
          />
          <Group justify="flex-end">
            <Button variant="subtle" onClick={onClose}>Cancel</Button>
            <Button
              type="submit"
              loading={createMutation.isPending}
            >
              Create
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
