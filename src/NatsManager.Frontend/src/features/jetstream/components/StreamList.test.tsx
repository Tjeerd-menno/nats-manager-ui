import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { StreamList } from './StreamList';

vi.mock('../hooks/useJetStream', () => ({
  useStreams: vi.fn(),
  useCreateStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useUpdateStream: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

import { useStreams } from '../hooks/useJetStream';
const mockUseStreams = vi.mocked(useStreams);

describe('StreamList', () => {
  const defaultProps = { onSelect: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    mockUseStreams.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useStreams>);

    renderWithProviders(<StreamList {...defaultProps} />);
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders streams when data loaded', () => {
    mockUseStreams.mockReturnValue({
      data: {
        items: [
          { name: 'orders', description: 'Order events', subjects: ['orders.>'], messages: 100, bytes: 2048, consumerCount: 2, retentionPolicy: 'Limits' },
        ],
        totalPages: 1,
        totalCount: 1,
        page: 1,
        pageSize: 25,
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useStreams>);

    renderWithProviders(<StreamList {...defaultProps} />);
    expect(screen.getByText('orders')).toBeInTheDocument();
  });

  it('shows empty state when no streams', () => {
    mockUseStreams.mockReturnValue({
      data: { items: [], totalPages: 0, totalCount: 0, page: 1, pageSize: 25 },
      isLoading: false,
    } as unknown as ReturnType<typeof useStreams>);

    renderWithProviders(<StreamList {...defaultProps} />);
    expect(screen.getByText(/no streams/i)).toBeInTheDocument();
  });
});
