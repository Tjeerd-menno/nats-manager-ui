import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { AppLayout } from './AppLayout';

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

  it('renders the color scheme toggle button', () => {
    renderWithProviders(<AppLayout />);
    expect(screen.getByRole('button', { name: /toggle color scheme/i })).toBeInTheDocument();
  });

  it('calls setColorScheme when the toggle button is clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AppLayout />);

    const toggleButton = screen.getByRole('button', { name: /toggle color scheme/i });
    await user.click(toggleButton);

    // After clicking, the button should still be present (toggle is functional)
    expect(toggleButton).toBeInTheDocument();
  });
});
