import { useState, useRef } from 'react';
import { Title, Text, Table, Badge, Card, Stack, Group, Button, TextInput, Modal, Loader, Center, ActionIcon, NumberInput } from '@mantine/core';
import { useObjectBuckets, useObjects, useObjectInfo, useCreateObjectBucket, useDeleteObjectBucket, useUploadObject, useDeleteObject } from './hooks/useObjectStore';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
import { useParams, useNavigate } from 'react-router-dom';
import { OpenRelationshipMapButton } from '../relationships/components/OpenRelationshipMapButton';
import { formatBytes, formatDateTime } from '../../shared/formatting';

export default function ObjectStorePage() {
  const { bucketName, objectName } = useParams();
  const { selectedEnvironmentId } = useEnvironmentContext();

  if (bucketName && objectName) {
    return <ObjectDetail environmentId={selectedEnvironmentId} bucketName={bucketName} objectName={objectName} />;
  }
  if (bucketName) {
    return <ObjectList environmentId={selectedEnvironmentId} bucketName={bucketName} />;
  }
  return <BucketList environmentId={selectedEnvironmentId} />;
}

function BucketList({ environmentId }: { environmentId: string | null }) {
  const navigate = useNavigate();
  const { data, isLoading } = useObjectBuckets(environmentId);
  const createMutation = useCreateObjectBucket(environmentId);
  const deleteMutation = useDeleteObjectBucket(environmentId);
  const [createOpen, setCreateOpen] = useState(false);
  const [bucketNameInput, setBucketNameInput] = useState('');
  const [description, setDescription] = useState('');
  const [maxSize, setMaxSize] = useState<number | string>('');

  if (!environmentId) {
    return (
      <div>
        <Title order={2}>Object Store</Title>
        <Text c="dimmed" mt="sm">Select an environment to view object store buckets.</Text>
      </div>
    );
  }

  if (isLoading) return <Center h={200}><Loader /></Center>;

  const buckets = data?.items ?? [];

  const handleCreate = () => {
    createMutation.mutate({
      bucketName: bucketNameInput,
      description: description || undefined,
      maxBucketSize: maxSize ? Number(maxSize) : undefined,
    }, { onSuccess: () => { setCreateOpen(false); setBucketNameInput(''); setDescription(''); setMaxSize(''); } });
  };

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Object Store</Title>
        <Button onClick={() => setCreateOpen(true)}>Create Bucket</Button>
      </Group>
      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Bucket</Table.Th>
            <Table.Th>Objects</Table.Th>
            <Table.Th>Total Size</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {buckets.map((bucket) => (
            <Table.Tr key={bucket.bucketName} style={{ cursor: 'pointer' }} onClick={() => navigate(`/objectstore/buckets/${bucket.bucketName}`)}>
              <Table.Td>{bucket.bucketName}</Table.Td>
              <Table.Td>{bucket.objectCount}</Table.Td>
              <Table.Td>{formatBytes(bucket.totalSize)}</Table.Td>
              <Table.Td>
                <ActionIcon color="red" variant="subtle" onClick={(e) => { e.stopPropagation(); deleteMutation.mutate(bucket.bucketName); }}>🗑️</ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <Modal opened={createOpen} onClose={() => setCreateOpen(false)} title="Create Object Bucket">
        <Stack>
          <TextInput label="Bucket Name" value={bucketNameInput} onChange={(e) => setBucketNameInput(e.currentTarget.value)} required />
          <TextInput label="Description" value={description} onChange={(e) => setDescription(e.currentTarget.value)} />
          <NumberInput label="Max Size (bytes)" value={maxSize} onChange={setMaxSize} />
          <Button onClick={handleCreate} loading={createMutation.isPending}>Create</Button>
        </Stack>
      </Modal>
    </Stack>
  );
}

function ObjectList({ environmentId, bucketName }: { environmentId: string | null; bucketName: string }) {
  const navigate = useNavigate();
  const { data, isLoading } = useObjects(environmentId, bucketName);
  const uploadMutation = useUploadObject(environmentId, bucketName);
  const deleteMutation = useDeleteObject(environmentId, bucketName);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [objectNameInput, setObjectNameInput] = useState('');
  const fileRef = useRef<HTMLInputElement>(null);

  if (isLoading) return <Center h={200}><Loader /></Center>;

  const objects = data?.items ?? [];

  const handleUpload = () => {
    const file = fileRef.current?.files?.[0];
    if (!file || !objectNameInput) return;
    uploadMutation.mutate({ objectName: objectNameInput, file }, {
      onSuccess: () => { setUploadOpen(false); setObjectNameInput(''); },
    });
  };

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Bucket: {bucketName}</Title>
        <Group>
          {environmentId && <OpenRelationshipMapButton environmentId={environmentId} resourceId={bucketName} resourceType="ObjectBucket" />}
          <Button onClick={() => setUploadOpen(true)}>Upload Object</Button>
        </Group>
      </Group>
      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Name</Table.Th>
            <Table.Th>Size</Table.Th>
            <Table.Th>Type</Table.Th>
            <Table.Th>Chunks</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {objects.map((objectInfo) => (
            <Table.Tr key={objectInfo.name} style={{ cursor: 'pointer' }} onClick={() => navigate(`/objectstore/buckets/${bucketName}/objects/${objectInfo.name}`)}>
              <Table.Td>{objectInfo.name}</Table.Td>
              <Table.Td>{formatBytes(objectInfo.size)}</Table.Td>
              <Table.Td><Badge size="sm">{objectInfo.contentType || 'unknown'}</Badge></Table.Td>
              <Table.Td>{objectInfo.chunks}</Table.Td>
              <Table.Td>
                <ActionIcon color="red" variant="subtle" onClick={(e) => { e.stopPropagation(); deleteMutation.mutate(objectInfo.name); }}>🗑️</ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <Modal opened={uploadOpen} onClose={() => setUploadOpen(false)} title="Upload Object">
        <Stack>
          <TextInput label="Object Name" value={objectNameInput} onChange={(e) => setObjectNameInput(e.currentTarget.value)} required />
          <input type="file" ref={fileRef} />
          <Button onClick={handleUpload} loading={uploadMutation.isPending}>Upload</Button>
        </Stack>
      </Modal>
    </Stack>
  );
}

function ObjectDetail({ environmentId, bucketName, objectName }: { environmentId: string | null; bucketName: string; objectName: string }) {
  const { data: objectInfo, isLoading } = useObjectInfo(environmentId, bucketName, objectName);

  if (isLoading) return <Center h={200}><Loader /></Center>;
  if (!objectInfo) return <Text c="red">Object not found</Text>;

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>{objectInfo.name}</Title>
        {environmentId && <OpenRelationshipMapButton environmentId={environmentId} resourceId={`${bucketName}/${objectName}`} resourceType="ObjectStoreObject" />}
      </Group>
      <Card shadow="sm" padding="lg" radius="md" withBorder>
        <Group>
          <div><Text size="sm" c="dimmed">Size</Text><Text fw={700}>{formatBytes(objectInfo.size)}</Text></div>
          <div><Text size="sm" c="dimmed">Content Type</Text><Text fw={700}>{objectInfo.contentType || 'N/A'}</Text></div>
          <div><Text size="sm" c="dimmed">Chunks</Text><Text fw={700}>{objectInfo.chunks}</Text></div>
          {objectInfo.digest && <div><Text size="sm" c="dimmed">Digest</Text><Text fw={700} size="xs">{objectInfo.digest}</Text></div>}
          {objectInfo.lastModified && <div><Text size="sm" c="dimmed">Modified</Text><Text fw={700}>{formatDateTime(objectInfo.lastModified)}</Text></div>}
        </Group>
      </Card>
      {objectInfo.description && <Text c="dimmed">{objectInfo.description}</Text>}
    </Stack>
  );
}
