import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import { GlobalSearch } from './SearchPage';

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

describe('GlobalSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders search input', () => {
    renderWithProviders(<GlobalSearch />);
    expect(screen.getByPlaceholderText('Search resources...')).toBeInTheDocument();
  });
});
