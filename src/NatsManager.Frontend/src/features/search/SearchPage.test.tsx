import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import { BookmarkList, GlobalSearch } from './SearchPage';
import { useBookmarks, useDeleteBookmark, useSearch } from './hooks/useSearch';

const navigate = vi.fn();

vi.mock('./hooks/useSearch', () => ({
  useSearch: vi.fn(() => ({ data: undefined, isLoading: false })),
  useBookmarks: vi.fn(() => ({ data: undefined, isLoading: false })),
  useDeleteBookmark: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useCreateBookmark: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}));

vi.mock('@mantine/hooks', async () => {
  const actual = await vi.importActual('@mantine/hooks');
  return {
    ...actual,
    useDebouncedValue: vi.fn((value: string) => [value]),
  };
});

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => navigate,
  };
});

const mockUseSearch = vi.mocked(useSearch);
const mockUseBookmarks = vi.mocked(useBookmarks);
const mockUseDeleteBookmark = vi.mocked(useDeleteBookmark);

describe('GlobalSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders search input', () => {
    renderWithProviders(<GlobalSearch />);
    expect(screen.getByPlaceholderText('Search resources...')).toBeInTheDocument();
  });

  it('shows an empty-state message when a search has no matches', async () => {
    const user = userEvent.setup();

    mockUseSearch.mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as ReturnType<typeof useSearch>);

    renderWithProviders(<GlobalSearch />);

    await user.type(screen.getByPlaceholderText('Search resources...'), 'or');

    expect(await screen.findByText('No results found')).toBeInTheDocument();
  });

  it('navigates to the matching resource when a result is selected', async () => {
    const user = userEvent.setup();

    mockUseSearch.mockReturnValue({
      data: [
        {
          resourceType: 'ObjectItem',
          resourceId: 'orders-bucket/incoming/orders/1.json',
          displayName: 'orders/1.json',
          description: 'Incoming order payload',
        },
      ],
      isLoading: false,
    } as unknown as ReturnType<typeof useSearch>);

    renderWithProviders(<GlobalSearch />);

    await user.type(screen.getByPlaceholderText('Search resources...'), 'or');
    await user.click(await screen.findByRole('button', { name: /orders\/1\.json/i }));

    expect(navigate).toHaveBeenCalledWith('/objectstore/buckets/orders-bucket/objects/incoming%2Forders%2F1.json');
  });
});

describe('BookmarkList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows a loader while bookmarks are loading', () => {
    mockUseBookmarks.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as unknown as ReturnType<typeof useBookmarks>);

    const { container } = renderWithProviders(<BookmarkList />);

    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('shows an empty state when no bookmarks exist', () => {
    mockUseBookmarks.mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as ReturnType<typeof useBookmarks>);

    renderWithProviders(<BookmarkList />);

    expect(screen.getByText('No bookmarks yet')).toBeInTheDocument();
  });

  it('navigates to bookmark targets and deletes bookmarks', async () => {
    const user = userEvent.setup();
    const deleteBookmark = vi.fn();

    mockUseBookmarks.mockReturnValue({
      data: [
        {
          id: 'bookmark-1',
          userId: 'user-1',
          resourceType: 'KvKey',
          resourceId: 'orders-bucket/customer/42',
          displayName: 'Customer 42',
          environmentId: 'env-1',
          createdAt: '2026-01-01T00:00:00Z',
        },
      ],
      isLoading: false,
    } as unknown as ReturnType<typeof useBookmarks>);
    mockUseDeleteBookmark.mockReturnValue({
      mutate: deleteBookmark,
      isPending: false,
    } as unknown as ReturnType<typeof useDeleteBookmark>);

    renderWithProviders(<BookmarkList />);

    await user.click(screen.getByRole('button', { name: /customer 42/i }));
    await user.click(screen.getByRole('button', { name: '✕' }));

    expect(navigate).toHaveBeenCalledWith('/kv/buckets/orders-bucket/keys/customer%2F42');
    expect(deleteBookmark).toHaveBeenCalledWith('bookmark-1');
  });
});
