import { useEffect } from 'react';
import { Modal, TextInput, Select, Switch, Textarea, Button, Group, Stack, NumberInput, Divider } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useRegisterEnvironment, useUpdateEnvironment, useEnvironment } from '../hooks/useEnvironments';
import type { CredentialType, EnvironmentListItem } from '../types';

interface EnvironmentFormProps {
  opened: boolean;
  onClose: () => void;
  environment?: EnvironmentListItem | null;
}

interface FormValues {
  name: string;
  description: string;
  serverUrl: string;
  credentialType: CredentialType;
  token: string;
  username: string;
  password: string;
  nkeyFile: string;
  credsFile: string;
  isProduction: boolean;
  isEnabled: boolean;
  monitoringUrl: string;
  monitoringPollingIntervalSeconds: number | '';
}

const credentialTypeOptions = [
  { value: 'None', label: 'None' },
  { value: 'Token', label: 'Token' },
  { value: 'UserPassword', label: 'User/Password' },
  { value: 'NKey', label: 'NKey' },
  { value: 'CredsFile', label: 'Credentials File' },
];

const allowedServerSchemes = new Set(['nats', 'tls', 'ws', 'wss']);
const serverUrlValidationMessage = 'Server URL must use nats://, tls://, ws://, or wss://. Use nats:// for standard TCP NATS endpoints.';

function normalizeServerUrl(value: string): string {
  return value.split(',').map((s) => s.trim()).filter(Boolean).join(',');
}

function validateServerUrl(value: string): string | null {
  const normalized = normalizeServerUrl(value);
  if (normalized.length === 0) return 'Server URL is required';

  const urls = normalized.split(',');
  for (const rawUrl of urls) {
    try {
      const url = new URL(rawUrl);
      const scheme = url.protocol.replace(':', '');
      if (!allowedServerSchemes.has(scheme) || url.hostname.length === 0) {
        return serverUrlValidationMessage;
      }
    } catch {
      return serverUrlValidationMessage;
    }
  }

  return null;
}

export function EnvironmentForm({ opened, onClose, environment }: EnvironmentFormProps) {
  const registerMutation = useRegisterEnvironment();
  const updateMutation = useUpdateEnvironment();
  const isEditing = !!environment;

  const { data: envDetail } = useEnvironment(environment?.id);

  const form = useForm<FormValues>({
    initialValues: {
      name: '',
      description: '',
      serverUrl: '',
      credentialType: 'None' as CredentialType,
      token: '',
      username: '',
      password: '',
      nkeyFile: '',
      credsFile: '',
      isProduction: false,
      isEnabled: true,
      monitoringUrl: '',
      monitoringPollingIntervalSeconds: '',
    },
    validate: {
      name: (value) => (value.trim().length === 0 ? 'Name is required' : null),
      serverUrl: validateServerUrl,
      username: (value, values) => (values.credentialType === 'UserPassword' && value.trim().length === 0 ? 'Username is required' : null),
      password: (value, values) => (values.credentialType === 'UserPassword' && value.trim().length === 0 ? 'Password is required' : null),
      token: (value, values) => (values.credentialType === 'Token' && value.trim().length === 0 ? 'Token is required' : null),
      nkeyFile: (value, values) => (values.credentialType === 'NKey' && value.trim().length === 0 ? 'NKey seed or file path is required' : null),
      credsFile: (value, values) => (values.credentialType === 'CredsFile' && value.trim().length === 0 ? 'Credentials file path is required' : null),
      monitoringUrl: (value) => {
        if (!value || value.trim().length === 0) return null;
        try {
          const url = new URL(value);
          if (url.protocol !== 'http:' && url.protocol !== 'https:') return 'Monitoring URL must use http:// or https://';
        } catch {
          return 'Monitoring URL must be a valid http:// or https:// URL';
        }
        return null;
      },
      monitoringPollingIntervalSeconds: (value) => {
        if (value === '' || value === undefined || value === null) return null;
        const n = Number(value);
        if (n < 5 || n > 300) return 'Polling interval must be between 5 and 300 seconds';
        return null;
      },
    },
  });

  useEffect(() => {
    if (environment && envDetail) {
      form.setValues({
        name: envDetail.name,
        description: envDetail.description ?? '',
        serverUrl: envDetail.serverUrl,
        credentialType: envDetail.credentialType,
        token: '',
        username: '',
        password: '',
        nkeyFile: '',
        credsFile: '',
        isProduction: envDetail.isProduction,
        isEnabled: envDetail.isEnabled,
        monitoringUrl: envDetail.monitoringUrl ?? '',
        monitoringPollingIntervalSeconds: envDetail.monitoringPollingIntervalSeconds ?? '',
      });
      form.resetDirty();
    } else if (!environment) {
      form.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [environment?.id, envDetail]);

  function buildCredential(values: FormValues): string | undefined {
    switch (values.credentialType) {
      case 'Token': return values.token || undefined;
      case 'UserPassword': return values.username && values.password ? `${values.username}:${values.password}` : undefined;
      case 'NKey': return values.nkeyFile || undefined;
      case 'CredsFile': return values.credsFile || undefined;
      default: return undefined;
    }
  }

  const handleSubmit = form.onSubmit(async (values) => {
    const credential = buildCredential(values);
    const serverUrl = normalizeServerUrl(values.serverUrl);
    if (isEditing) {
      await updateMutation.mutateAsync({
        id: environment.id,
        name: values.name,
        description: values.description || undefined,
        serverUrl,
        credentialType: values.credentialType,
        credential,
        isProduction: values.isProduction,
        isEnabled: values.isEnabled,
        monitoringUrl: values.monitoringUrl || null,
        monitoringPollingIntervalSeconds: values.monitoringPollingIntervalSeconds !== '' ? Number(values.monitoringPollingIntervalSeconds) : null,
      });
    } else {
      await registerMutation.mutateAsync({
        name: values.name,
        description: values.description || undefined,
        serverUrl,
        credentialType: values.credentialType,
        credential,
        isProduction: values.isProduction,
      });
    }
    form.reset();
    onClose();
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEditing ? 'Edit Environment' : 'Register Environment'}
      size="lg"
    >
      <form onSubmit={handleSubmit}>
        <Stack>
          <TextInput
            label="Name"
            placeholder="production-us-east"
            required
            {...form.getInputProps('name')}
          />
          <Textarea
            label="Description"
            placeholder="Optional description"
            {...form.getInputProps('description')}
          />
          <TextInput
            label="Server URL"
            placeholder="nats://localhost:4222"
            description="Use nats:// for Aspire TCP endpoints, keeping the same host and port."
            required
            {...form.getInputProps('serverUrl')}
          />
          <Select
            label="Credential Type"
            data={credentialTypeOptions}
            {...form.getInputProps('credentialType')}
          />
          {form.values.credentialType === 'Token' && (
            <TextInput
              label="Token"
              placeholder="Authentication token"
              type="password"
              {...form.getInputProps('token')}
            />
          )}
          {form.values.credentialType === 'UserPassword' && (
            <>
              <TextInput
                label="Username"
                placeholder="nats-user"
                {...form.getInputProps('username')}
              />
              <TextInput
                label="Password"
                placeholder="Enter password"
                type="password"
                {...form.getInputProps('password')}
              />
            </>
          )}
          {form.values.credentialType === 'NKey' && (
            <TextInput
              label="NKey Seed or File Path"
              placeholder="/path/to/nkey.seed"
              {...form.getInputProps('nkeyFile')}
            />
          )}
          {form.values.credentialType === 'CredsFile' && (
            <TextInput
              label="Credentials File Path"
              placeholder="/path/to/creds.jwt"
              {...form.getInputProps('credsFile')}
            />
          )}
          <Switch
            label="Production environment"
            description="Enables additional safeguards for destructive operations"
            {...form.getInputProps('isProduction', { type: 'checkbox' })}
          />
          {isEditing && (
            <Switch
              label="Enabled"
              description="Disabled environments will not be connected"
              {...form.getInputProps('isEnabled', { type: 'checkbox' })}
            />
          )}
          <Divider label="Monitoring (optional)" labelPosition="left" />
          <TextInput
            label="Monitoring URL"
            placeholder="http://localhost:8222"
            description="NATS HTTP monitoring endpoint URL"
            {...form.getInputProps('monitoringUrl')}
          />
          <NumberInput
            label="Polling Interval (seconds)"
            placeholder="30"
            description="Override default polling interval (5–300 seconds)"
            min={5}
            max={300}
            {...form.getInputProps('monitoringPollingIntervalSeconds')}
          />
          <Group justify="flex-end">
            <Button variant="subtle" onClick={onClose}>Cancel</Button>
            <Button
              type="submit"
              loading={registerMutation.isPending || updateMutation.isPending}
            >
              {isEditing ? 'Update' : 'Register'}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
