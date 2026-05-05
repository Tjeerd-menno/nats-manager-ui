import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { useEnvironments } from '../hooks/useEnvironments';
import { EnvironmentSelector } from './EnvironmentSelector';

vi.mock('../hooks/useEnvironments', () => ({
  useEnvironments: vi.fn(),
}));

const mockUseEnvironments = vi.mocked(useEnvironments);

describe('EnvironmentSelector', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders select input', () => {
    mockUseEnvironments.mockReturnValue({
      data: {
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 100,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false,
        },
        freshness: { freshness: 'live', timestamp: '2026-05-05T00:00:00Z' },
      },
    } as unknown as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentSelector selectedId={null} onSelect={vi.fn()} />);

    expect(screen.getByPlaceholderText('Select environment')).toBeInTheDocument();
  });

  it('auto-selects the only available environment when none is selected', async () => {
    const onSelect = vi.fn();
    mockUseEnvironments.mockReturnValue({
      data: {
        data: {
          items: [{
            id: 'env-1',
            name: 'local-dev',
            description: '',
            isEnabled: true,
            isProduction: false,
            connectionStatus: 'Available',
            lastSuccessfulContact: null,
          }],
          totalCount: 1,
          page: 1,
          pageSize: 100,
          totalPages: 1,
          hasNext: false,
          hasPrevious: false,
        },
        freshness: { freshness: 'live', timestamp: '2026-05-05T00:00:00Z' },
      },
    } as unknown as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentSelector selectedId={null} onSelect={onSelect} />);

    await waitFor(() => expect(onSelect).toHaveBeenCalledWith('env-1'));
  });

  it('does not auto-select when multiple environments are available', async () => {
    const onSelect = vi.fn();
    mockUseEnvironments.mockReturnValue({
      data: {
        data: {
          items: [
            {
              id: 'env-1',
              name: 'local-dev',
              description: '',
              isEnabled: true,
              isProduction: false,
              connectionStatus: 'Available',
              lastSuccessfulContact: null,
            },
            {
              id: 'env-2',
              name: 'staging',
              description: '',
              isEnabled: true,
              isProduction: false,
              connectionStatus: 'Unknown',
              lastSuccessfulContact: null,
            },
          ],
          totalCount: 2,
          page: 1,
          pageSize: 100,
          totalPages: 1,
          hasNext: false,
          hasPrevious: false,
        },
        freshness: { freshness: 'live', timestamp: '2026-05-05T00:00:00Z' },
      },
    } as unknown as ReturnType<typeof useEnvironments>);

    renderWithProviders(<EnvironmentSelector selectedId={null} onSelect={onSelect} />);

    await waitFor(() => expect(onSelect).not.toHaveBeenCalled());
  });
});
