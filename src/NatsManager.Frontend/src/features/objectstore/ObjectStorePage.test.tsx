import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import ObjectStorePage from './ObjectStorePage';

vi.mock('./hooks/useObjectStore', () => ({
  useObjectBuckets: vi.fn(),
  useObjects: vi.fn(() => ({ data: undefined, isLoading: false })),
  useObjectInfo: vi.fn(() => ({ data: undefined, isLoading: false })),
  useCreateObjectBucket: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useDeleteObjectBucket: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useUploadObject: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
  useDeleteObject: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}));

vi.mock('../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useParams: vi.fn(() => ({})),
    useNavigate: vi.fn(() => vi.fn()),
  };
});

import { useObjectBuckets } from './hooks/useObjectStore';
import { useEnvironmentContext } from '../environments/EnvironmentContext';
const mockUseObjectBuckets = vi.mocked(useObjectBuckets);
const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);

describe('ObjectStorePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows select environment message when no env selected', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: null,
      selectEnvironment: vi.fn(),
    });
    mockUseObjectBuckets.mockReturnValue({
      data: undefined,
      isLoading: false,
    } as unknown as ReturnType<typeof useObjectBuckets>);

    renderWithProviders(<ObjectStorePage />);
    expect(screen.getByText('Select an environment to view object store buckets.')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseObjectBuckets.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as unknown as ReturnType<typeof useObjectBuckets>);

    const { container } = renderWithProviders(<ObjectStorePage />);
    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('renders bucket list with data', () => {
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseObjectBuckets.mockReturnValue({
      data: {
        items: [
          { bucketName: 'assets', objectCount: 10, totalSize: 2048, description: '' },
          { bucketName: 'backups', objectCount: 3, totalSize: 5120, description: 'Backup files' },
        ],
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useObjectBuckets>);

    renderWithProviders(<ObjectStorePage />);
    expect(screen.getByText('assets')).toBeInTheDocument();
    expect(screen.getByText('backups')).toBeInTheDocument();
  });
});
