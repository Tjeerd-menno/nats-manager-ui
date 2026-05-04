import { useEffect } from 'react';
import { Modal, TextInput, Textarea, Stack, Button, Group, Alert } from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconAlertCircle } from '@tabler/icons-react';
import { usePutKvKey } from '../hooks/useKv';
import { useEnvironmentContext } from '../../environments/EnvironmentContext';
import { validateNonEmpty } from '../../../shared/validation';

interface KvKeyEditorProps {
  opened: boolean;
  onClose: () => void;
  bucketName: string;
  editKey?: string;
  editValue?: string;
  editRevision?: number;
}

export function KvKeyEditor({ opened, onClose, bucketName, editKey, editValue, editRevision }: KvKeyEditorProps) {
  const { selectedEnvironmentId } = useEnvironmentContext();
  const putKey = usePutKvKey(selectedEnvironmentId, bucketName);
  const isEdit = !!editKey;

  const form = useForm({
    initialValues: {
      key: editKey ?? '',
      value: editValue ?? '',
    },
    validate: {
      key: (v) => validateNonEmpty(v, 'Key'),
    },
  });

  // Reset form values when modal opens
  useEffect(() => {
    if (opened) {
      form.setValues({
        key: editKey ?? '',
        value: editValue ?? '',
      });
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened, editKey, editValue]);

  const handleSubmit = form.onSubmit((values) => {
    putKey.mutate(
      {
        key: values.key,
        value: btoa(values.value),
        expectedRevision: isEdit ? editRevision : undefined,
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
    <Modal opened={opened} onClose={onClose} title={isEdit ? `Edit Key: ${editKey}` : 'Create Key'}>
      <form onSubmit={handleSubmit}>
        <Stack>
          {isEdit && editRevision !== undefined && (
            <Alert icon={<IconAlertCircle size={16} />} color="yellow" variant="light">
              This will update revision {editRevision}. If the key has been modified since, the update will fail.
            </Alert>
          )}
          <TextInput
            label="Key"
            required
            disabled={isEdit}
            {...form.getInputProps('key')}
          />
          <Textarea
            label="Value"
            autosize
            minRows={4}
            maxRows={12}
            {...form.getInputProps('value')}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>Cancel</Button>
            <Button type="submit" loading={putKey.isPending}>
              {isEdit ? 'Update' : 'Create'}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
