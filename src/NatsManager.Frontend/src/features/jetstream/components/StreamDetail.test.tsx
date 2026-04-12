import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { StreamDetail } from './StreamDetail';

vi.mock('../hooks/useJetStream', () => ({
  useStream: vi.fn(),
  useDeleteStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  usePurgeStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useCreateStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useUpdateStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useCreateConsumer: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useDeleteConsumer: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useStreamMessages: vi.fn(() => ({ data: undefined, isLoading: false })),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

import { useStream } from '../hooks/useJetStream';
const mockUseStream = vi.mocked(useStream);

describe('StreamDetail', () => {
  const defaultProps = {
    streamName: 'orders',
    onConsumerSelect: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    mockUseStream.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useStream>);

    renderWithProviders(<StreamDetail {...defaultProps} />);
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders stream info when loaded', () => {
    mockUseStream.mockReturnValue({
      data: {
        info: {
          name: 'orders',
          description: 'Order events',
          subjects: ['orders.>'],
          retentionPolicy: 'Limits',
          storageType: 'File',
          messages: 100,
          bytes: 2048,
          consumerCount: 2,
          created: new Date().toISOString(),
          state: { messages: 100, bytes: 2048, firstSeq: 1, lastSeq: 100, firstTimestamp: null, lastTimestamp: null },
        },
        config: {
          name: 'orders',
          description: 'Order events',
          subjects: ['orders.>'],
          retentionPolicy: 'Limits',
          maxMessages: -1,
          maxBytes: -1,
          maxAge: 0,
          storageType: 'File',
          replicas: 1,
          discardPolicy: 'Old',
          maxMsgSize: -1,
          denyDelete: false,
          denyPurge: false,
          allowRollup: false,
        },
        consumers: [],
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useStream>);

    renderWithProviders(<StreamDetail {...defaultProps} />);
    expect(screen.getByText('orders')).toBeInTheDocument();
  });
});
