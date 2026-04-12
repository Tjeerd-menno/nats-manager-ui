import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { KvKeyDetail } from './KvKeyDetail';

vi.mock('../hooks/useKv', () => ({
  useKvKey: vi.fn(),
  useKvKeyHistory: vi.fn(() => ({ data: undefined, isLoading: false })),
  useDeleteKvKey: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  usePutKvKey: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
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

import { useKvKey } from '../hooks/useKv';
const mockUseKvKey = vi.mocked(useKvKey);

describe('KvKeyDetail', () => {
  const props = { bucketName: 'config', keyName: 'app.setting' };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    mockUseKvKey.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useKvKey>);

    renderWithProviders(<KvKeyDetail {...props} />);
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders key detail', () => {
    mockUseKvKey.mockReturnValue({
      data: {
        data: {
          key: 'app.setting',
          value: '{"debug": true}',
          revision: 3,
          created: new Date().toISOString(),
          operation: 'Put',
        },
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useKvKey>);

    renderWithProviders(<KvKeyDetail {...props} />);
    expect(screen.getByText('app.setting')).toBeInTheDocument();
  });
});
