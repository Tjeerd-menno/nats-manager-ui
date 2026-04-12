import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { ConsumerDetail } from './ConsumerDetail';

vi.mock('../hooks/useJetStream', () => ({
  useConsumer: vi.fn(),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

import { useConsumer } from '../hooks/useJetStream';
const mockUseConsumer = vi.mocked(useConsumer);

describe('ConsumerDetail', () => {
  const props = { streamName: 'orders', consumerName: 'processor' };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading when data not ready', () => {
    mockUseConsumer.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useConsumer>);

    renderWithProviders(<ConsumerDetail {...props} />);
    expect(screen.getByText('Loading consumer details...')).toBeInTheDocument();
  });

  it('renders consumer info', () => {
    mockUseConsumer.mockReturnValue({
      data: {
        name: 'processor',
        description: 'Order processor',
        streamName: 'orders',
        deliverPolicy: 'All',
        ackPolicy: 'Explicit',
        replayPolicy: 'Instant',
        numPending: 5,
        numAckPending: 2,
        numRedelivered: 0,
        isHealthy: true,
        lastDelivered: null,
        created: new Date().toISOString(),
        state: {
          delivered: 100,
          ackFloor: 95,
          numAckPending: 2,
          numRedelivered: 0,
          numPending: 5,
          numWaiting: 0,
          lastDelivered: new Date().toISOString(),
        },
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useConsumer>);

    renderWithProviders(<ConsumerDetail {...props} />);
    expect(screen.getByText('processor')).toBeInTheDocument();
    expect(screen.getByText('Healthy')).toBeInTheDocument();
  });
});
