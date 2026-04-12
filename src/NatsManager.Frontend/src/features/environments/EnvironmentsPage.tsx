import { useState } from 'react';
import { Title, Stack } from '@mantine/core';
import { useParams } from 'react-router-dom';
import { EnvironmentList } from './components/EnvironmentList';
import { EnvironmentForm } from './components/EnvironmentForm';
import { EnvironmentDetail } from './components/EnvironmentDetail';
import type { EnvironmentListItem } from './types';

export default function EnvironmentsPage() {
  const { id } = useParams<{ id: string }>();
  const [formOpened, setFormOpened] = useState(false);
  const [editingEnv, setEditingEnv] = useState<EnvironmentListItem | null>(null);

  if (id) {
    return <EnvironmentDetail environmentId={id} />;
  }

  return (
    <Stack>
      <Title order={2}>Environments</Title>
      <EnvironmentList
        onCreate={() => {
          setEditingEnv(null);
          setFormOpened(true);
        }}
        onEdit={(env) => {
          setEditingEnv(env);
          setFormOpened(true);
        }}
        onSelect={(env) => {
          window.location.href = `/environments/${env.id}`;
        }}
      />
      <EnvironmentForm
        opened={formOpened}
        onClose={() => setFormOpened(false)}
        environment={editingEnv}
      />
    </Stack>
  );
}
