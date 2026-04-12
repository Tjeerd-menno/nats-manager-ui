import { useParams, useNavigate } from 'react-router-dom';
import { Title, Stack, Breadcrumbs, Anchor } from '@mantine/core';
import { StreamList } from './components/StreamList';
import { StreamDetail } from './components/StreamDetail';
import { ConsumerDetail } from './components/ConsumerDetail';

export default function JetStreamPage() {
  const { streamName, consumerName } = useParams<{ streamName: string; consumerName: string }>();
  const navigate = useNavigate();

  if (consumerName && streamName) {
    return (
      <Stack>
        <Breadcrumbs>
          <Anchor onClick={() => navigate('/jetstream/streams')}>Streams</Anchor>
          <Anchor onClick={() => navigate(`/jetstream/streams/${streamName}`)}>{streamName}</Anchor>
          <span>{consumerName}</span>
        </Breadcrumbs>
        <ConsumerDetail streamName={streamName} consumerName={consumerName} />
      </Stack>
    );
  }

  if (streamName) {
    return (
      <Stack>
        <Breadcrumbs>
          <Anchor onClick={() => navigate('/jetstream/streams')}>Streams</Anchor>
          <span>{streamName}</span>
        </Breadcrumbs>
        <StreamDetail
          streamName={streamName}
          onConsumerSelect={(name) => navigate(`/jetstream/streams/${streamName}/consumers/${name}`)}
          onDeleted={() => navigate('/jetstream/streams')}
        />
      </Stack>
    );
  }

  return (
    <Stack>
      <Title order={2}>JetStream Streams</Title>
      <StreamList onSelect={(name) => navigate(`/jetstream/streams/${name}`)} />
    </Stack>
  );
}
