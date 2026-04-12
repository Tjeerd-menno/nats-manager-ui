import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { KvBucketList } from './KvBucketList';

vi.mock('../hooks/useKv', () => ({
  useKvBuckets: vi.fn(),
  useDeleteKvBucket: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useCreateKvBucket: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

vi.mock('@mantine/modals', () => ({
  modals: { openConfirmModal: vi.fn() },
}));

import { useKvBuckets } from '../hooks/useKv';
const mockUseKvBuckets = vi.mocked(useKvBuckets);

describe('KvBucketList', () => {
  const defaultProps = { onSelect: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    mockUseKvBuckets.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useKvBuckets>);

    renderWithProviders(<KvBucketList {...defaultProps} />);
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders buckets', () => {
    mockUseKvBuckets.mockReturnValue({
      data: [
        { bucketName: 'config', entries: 10, bytes: 1024, description: 'Config data' },
        { bucketName: 'sessions', entries: 5, bytes: 512, description: '' },
      ],
      isLoading: false,
    } as unknown as ReturnType<typeof useKvBuckets>);

    renderWithProviders(<KvBucketList {...defaultProps} />);
    expect(screen.getByText('config')).toBeInTheDocument();
    expect(screen.getByText('sessions')).toBeInTheDocument();
  });

  it('shows empty state when no buckets', () => {
    mockUseKvBuckets.mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as ReturnType<typeof useKvBuckets>);

    renderWithProviders(<KvBucketList {...defaultProps} />);
    expect(screen.getByText(/no.*bucket/i)).toBeInTheDocument();
  });
});
