import { useParams, useNavigate } from 'react-router-dom';
import { Title, Stack, Breadcrumbs, Anchor } from '@mantine/core';
import { KvBucketList } from './components/KvBucketList';
import { KvBucketDetail } from './components/KvBucketDetail';
import { KvKeyDetail } from './components/KvKeyDetail';

export default function KvPage() {
  const { bucketName, keyName } = useParams<{ bucketName: string; keyName: string }>();
  const navigate = useNavigate();

  if (keyName && bucketName) {
    return (
      <Stack>
        <Breadcrumbs>
          <Anchor onClick={() => navigate('/kv/buckets')}>KV Buckets</Anchor>
          <Anchor onClick={() => navigate(`/kv/buckets/${bucketName}`)}>{bucketName}</Anchor>
          <span>{keyName}</span>
        </Breadcrumbs>
        <KvKeyDetail bucketName={bucketName} keyName={keyName} />
      </Stack>
    );
  }

  if (bucketName) {
    return (
      <Stack>
        <Breadcrumbs>
          <Anchor onClick={() => navigate('/kv/buckets')}>KV Buckets</Anchor>
          <span>{bucketName}</span>
        </Breadcrumbs>
        <KvBucketDetail
          bucketName={bucketName}
          onKeySelect={(key) => navigate(`/kv/buckets/${bucketName}/keys/${key}`)}
        />
      </Stack>
    );
  }

  return (
    <Stack>
      <Title order={2}>Key-Value Store</Title>
      <KvBucketList onSelect={(name) => navigate(`/kv/buckets/${name}`)} />
    </Stack>
  );
}
