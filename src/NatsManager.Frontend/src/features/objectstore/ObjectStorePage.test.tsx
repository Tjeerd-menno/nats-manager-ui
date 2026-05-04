import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import ObjectStorePage from './ObjectStorePage';
import { useCreateObjectBucket, useObjects, useUploadObject } from './hooks/useObjectStore';

vi.mock('./hooks/useObjectStore', () => ({
  useObjectBuckets: vi.fn(),
  useObjects: vi.fn(() => ({ data: undefined, isLoading: false })),
  useObjectInfo: vi.fn(() => ({ data: undefined, isLoading: false })),
  useCreateObjectBucket: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useDeleteObjectBucket: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useUploadObject: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useDeleteObject: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}));

vi.mock('../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(),
}));

vi.mock('../relationships/components/OpenRelationshipMapButton', () => ({
  OpenRelationshipMapButton: () => null,
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
import { useParams } from 'react-router-dom';
const mockUseObjectBuckets = vi.mocked(useObjectBuckets);
const mockUseObjects = vi.mocked(useObjects);
const mockUseEnvironmentContext = vi.mocked(useEnvironmentContext);
const mockUseParams = vi.mocked(useParams);

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

  it('validates object bucket name before submitting', async () => {
    const user = userEvent.setup();
    const createBucket = vi.fn();
    vi.mocked(useCreateObjectBucket).mockReturnValue({
      mutate: createBucket,
      isPending: false,
    } as unknown as ReturnType<typeof useCreateObjectBucket>);
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseObjectBuckets.mockReturnValue({
      data: { items: [] },
      isLoading: false,
    } as unknown as ReturnType<typeof useObjectBuckets>);

    renderWithProviders(<ObjectStorePage />);

    await user.click(screen.getByRole('button', { name: 'Create Bucket' }));
    await waitFor(() => expect(screen.getByText('Create Object Bucket')).toBeInTheDocument());
    await user.type(screen.getAllByRole('textbox')[0], 'bad bucket');
    await user.click(screen.getByRole('button', { name: 'Create' }));

    expect(await screen.findByText('Bucket name can only contain letters, numbers, dots, hyphens, and underscores')).toBeInTheDocument();
    expect(createBucket).not.toHaveBeenCalled();
  });

  it('blocks upload when object name contains a path separator', async () => {
    const user = userEvent.setup();
    const upload = vi.fn();
    vi.mocked(useUploadObject).mockReturnValue({
      mutate: upload,
      isPending: false,
    } as unknown as ReturnType<typeof useUploadObject>);
    mockUseParams.mockReturnValue({ bucketName: 'assets' });
    mockUseEnvironmentContext.mockReturnValue({
      selectedEnvironmentId: 'env-1',
      selectEnvironment: vi.fn(),
    });
    mockUseObjects.mockReturnValue({
      data: { items: [] },
      isLoading: false,
    } as unknown as ReturnType<typeof useObjects>);

    renderWithProviders(<ObjectStorePage />);

    await user.click(screen.getByRole('button', { name: 'Upload Object' }));
    await waitFor(() => expect(screen.getByText('Upload Object', { selector: 'h2' })).toBeInTheDocument());

    const nameInput = screen.getByRole('textbox', { name: /object name/i });
    await user.type(nameInput, 'path/to/file.txt');

    expect(await screen.findByText('Object name contains invalid path characters')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Upload' })).toBeDisabled();
    expect(upload).not.toHaveBeenCalled();
  });
});
