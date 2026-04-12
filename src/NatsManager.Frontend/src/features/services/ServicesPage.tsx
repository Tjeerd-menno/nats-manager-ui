import { useState } from 'react';
import { Title, Text, Table, Badge, Card, Stack, Group, Button, TextInput, Modal, Textarea, Loader, Center, Code } from '@mantine/core';
import { useServices, useService, useTestService } from './hooks/useServices';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { useParams, useNavigate } from 'react-router-dom';

export default function ServicesPage() {
  const { serviceName } = useParams();
  const { selectedEnvironmentId } = useEnvironmentContext();

  if (serviceName) {
    return <ServiceDetail environmentId={selectedEnvironmentId} serviceName={serviceName} />;
  }

  return <ServiceList environmentId={selectedEnvironmentId} />;
}

function ServiceList({ environmentId }: { environmentId: string | null }) {
  const navigate = useNavigate();
  const { data: services, isLoading } = useServices(environmentId);

  if (!environmentId) {
    return (
      <div>
        <Title order={2}>Services</Title>
        <Text c="dimmed" mt="sm">Select an environment to view services.</Text>
      </div>
    );
  }

  if (isLoading) return <Center h={200}><Loader /></Center>;

  return (
    <Stack>
      <Title order={2}>Services</Title>
      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Version</Table.Th>
            <Table.Th>Endpoints</Table.Th>
            <Table.Th>Requests</Table.Th>
            <Table.Th>Errors</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {services?.map((svc) => (
            <Table.Tr key={svc.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/services/${svc.name}`)}>
              <Table.Td>{svc.name}</Table.Td>
              <Table.Td><Badge size="sm">{svc.version}</Badge></Table.Td>
              <Table.Td>{svc.endpoints.length}</Table.Td>
              <Table.Td>{svc.stats.totalRequests}</Table.Td>
              <Table.Td>
                {svc.stats.totalErrors > 0
                  ? <Badge color="red">{svc.stats.totalErrors}</Badge>
                  : <Badge color="green">0</Badge>}
              </Table.Td>
            </Table.Tr>
          ))}
          {services?.length === 0 && (
            <Table.Tr><Table.Td colSpan={5}><Text c="dimmed" ta="center">No services discovered</Text></Table.Td></Table.Tr>
          )}
        </Table.Tbody>
      </Table>
    </Stack>
  );
}

function ServiceDetail({ environmentId, serviceName }: { environmentId: string | null; serviceName: string }) {
  const { data: service, isLoading } = useService(environmentId, serviceName);
  const testMutation = useTestService(environmentId);
  const [testModalOpen, setTestModalOpen] = useState(false);
  const [testSubject, setTestSubject] = useState('');
  const [testPayload, setTestPayload] = useState('');

  if (isLoading) return <Center h={200}><Loader /></Center>;

  if (!service) {
    return <Text c="red">Service not found</Text>;
  }

  const handleTest = () => {
    testMutation.mutate({ serviceName, subject: testSubject, payload: testPayload || undefined });
  };

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>{service.name}</Title>
        <Button onClick={() => setTestModalOpen(true)}>Test Request</Button>
      </Group>

      <Text c="dimmed">{service.description}</Text>

      <Card shadow="sm" padding="lg" radius="md" withBorder>
        <Group>
          <div>
            <Text size="sm" c="dimmed">Version</Text>
            <Badge>{service.version}</Badge>
          </div>
          <div>
            <Text size="sm" c="dimmed">Total Requests</Text>
            <Text fw={700}>{service.stats.totalRequests}</Text>
          </div>
          <div>
            <Text size="sm" c="dimmed">Errors</Text>
            <Text fw={700} c={service.stats.totalErrors > 0 ? 'red' : undefined}>{service.stats.totalErrors}</Text>
          </div>
        </Group>
      </Card>

      <Title order={4}>Endpoints</Title>
      <Table striped>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Subject</Table.Th>
            <Table.Th>Queue Group</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {service.endpoints.map((ep) => (
            <Table.Tr key={ep.name}>
              <Table.Td>{ep.name}</Table.Td>
              <Table.Td><Code>{ep.subject}</Code></Table.Td>
              <Table.Td>{ep.queueGroup || '—'}</Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <Modal opened={testModalOpen} onClose={() => setTestModalOpen(false)} title="Test Service Request">
        <Stack>
          <TextInput label="Subject" value={testSubject} onChange={(e) => setTestSubject(e.currentTarget.value)} required />
          <Textarea label="Payload" value={testPayload} onChange={(e) => setTestPayload(e.currentTarget.value)} minRows={3} />
          <Button onClick={handleTest} loading={testMutation.isPending}>Send</Button>
          {testMutation.data && (
            <div>
              <Text size="sm" fw={500}>Response:</Text>
              <Code block>{testMutation.data}</Code>
            </div>
          )}
          {testMutation.error && (
            <Text c="red" size="sm">Error: {(testMutation.error as Error).message}</Text>
          )}
        </Stack>
      </Modal>
    </Stack>
  );
}
