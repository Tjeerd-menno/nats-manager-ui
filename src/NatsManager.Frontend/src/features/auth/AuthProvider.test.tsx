import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import { AuthProvider } from './AuthProvider';
import { useAuth } from './useAuth';
import { apiClient } from '../../api/client';
import { useLocation } from 'react-router-dom';

vi.mock('../../api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

function AuthStateProbe() {
  const { user, isAuthenticated, isLoading } = useAuth();

  return (
    <div>
      <span data-testid="loading">{isLoading ? 'loading' : 'ready'}</span>
      <span data-testid="authenticated">{isAuthenticated ? 'yes' : 'no'}</span>
      <span data-testid="username">{user?.username ?? 'anonymous'}</span>
    </div>
  );
}

function RouteAwareAuthProvider() {
  const location = useLocation();
  const skipCurrentUserBootstrap = location.pathname === '/login';
  const authProviderKey = skipCurrentUserBootstrap ? 'public' : 'protected';

  return (
    <AuthProvider
      key={authProviderKey}
      skipCurrentUserBootstrap={skipCurrentUserBootstrap}
    >
      <AuthStateProbe />
    </AuthProvider>
  );
}

describe('AuthProvider', () => {
  const mockGet = vi.mocked(apiClient.get);

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('skips the current-user bootstrap request on the login route', async () => {
    renderWithProviders(
      <RouteAwareAuthProvider />,
      { route: '/login' },
    );

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('ready');
    });

    expect(screen.getByTestId('authenticated')).toHaveTextContent('no');
    expect(screen.getByTestId('username')).toHaveTextContent('anonymous');
    expect(mockGet).not.toHaveBeenCalled();
  });

  it('loads the current user on protected routes', async () => {
    mockGet.mockResolvedValue({
      data: {
        id: 'user-1',
        username: 'admin',
        displayName: 'Administrator',
        roles: ['Administrator'],
      },
    });

    renderWithProviders(
      <RouteAwareAuthProvider />,
      { route: '/dashboard' },
    );

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('ready');
    });

    expect(mockGet).toHaveBeenCalledWith('/auth/me');
    expect(screen.getByTestId('authenticated')).toHaveTextContent('yes');
    expect(screen.getByTestId('username')).toHaveTextContent('admin');
  });

  it('keeps the user anonymous when the current-user endpoint returns null', async () => {
    mockGet.mockResolvedValue({ data: null });

    renderWithProviders(
      <RouteAwareAuthProvider />,
      { route: '/dashboard' },
    );

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('ready');
    });

    expect(screen.getByTestId('authenticated')).toHaveTextContent('no');
    expect(screen.getByTestId('username')).toHaveTextContent('anonymous');
  });
});
