import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { AppLayout } from './AppLayout';

const mockSetColorScheme = vi.fn();
const mockUseComputedColorScheme = vi.fn(() => 'light' as 'light' | 'dark');

vi.mock('@mantine/core', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@mantine/core')>();
  return {
    ...actual,
    useMantineColorScheme: vi.fn(() => ({
      setColorScheme: mockSetColorScheme,
      colorScheme: 'auto' as const,
      toggleColorScheme: vi.fn(),
    })),
    useComputedColorScheme: () => mockUseComputedColorScheme(),
  };
});

vi.mock('../features/auth/useAuth', () => ({
  useAuth: vi.fn(() => ({
    user: { displayName: 'Test User' },
    logout: vi.fn(),
    hasRole: vi.fn(() => false),
    isAuthenticated: true,
    isLoading: false,
  })),
}));

vi.mock('../features/environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: null,
    selectEnvironment: vi.fn(),
  })),
}));

vi.mock('../features/environments/components/EnvironmentSelector', () => ({
  EnvironmentSelector: () => null,
}));

vi.mock('../features/search/SearchPage', () => ({
  GlobalSearch: () => null,
}));

describe('AppLayout color scheme toggle', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows moon icon and calls setColorScheme("dark") when current scheme is light', async () => {
    mockUseComputedColorScheme.mockReturnValue('light');
    const user = userEvent.setup();

    renderWithProviders(<AppLayout />);

    expect(screen.getByTestId('icon-moon')).toBeInTheDocument();
    expect(screen.queryByTestId('icon-sun')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /toggle color scheme/i }));

    expect(mockSetColorScheme).toHaveBeenCalledOnce();
    expect(mockSetColorScheme).toHaveBeenCalledWith('dark');
  });

  it('shows sun icon and calls setColorScheme("light") when current scheme is dark', async () => {
    mockUseComputedColorScheme.mockReturnValue('dark');
    const user = userEvent.setup();

    renderWithProviders(<AppLayout />);

    expect(screen.getByTestId('icon-sun')).toBeInTheDocument();
    expect(screen.queryByTestId('icon-moon')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /toggle color scheme/i }));

    expect(mockSetColorScheme).toHaveBeenCalledOnce();
    expect(mockSetColorScheme).toHaveBeenCalledWith('light');
  });
});
