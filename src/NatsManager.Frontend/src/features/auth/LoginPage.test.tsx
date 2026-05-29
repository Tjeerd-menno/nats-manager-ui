import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import { LoginPage } from './LoginPage';
import type { AuthConfig } from './types';

const mockLogin = vi.fn();
const mockLoginWithOidc = vi.fn();
let mockAuthConfig: AuthConfig = {
  oidcEnabled: false,
  oidcLoginPath: '/api/auth/oidc/login',
};

vi.mock('./useAuth', () => ({
  useAuth: vi.fn(() => ({
    login: mockLogin,
    loginWithOidc: mockLoginWithOidc,
    logout: vi.fn(),
    user: null,
    isAuthenticated: false,
    isLoading: false,
    authConfig: mockAuthConfig,
    hasRole: vi.fn(() => false),
  })),
}));

vi.mock('../../api/client', () => ({
  extractProblemDetails: vi.fn(() => null),
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockAuthConfig = {
      oidcEnabled: false,
      oidcLoginPath: '/api/auth/oidc/login',
    };
  });

  it('renders login form', () => {
    renderWithProviders(<LoginPage />);
    expect(screen.getByText('NATS Manager')).toBeInTheDocument();
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('calls login on form submission', async () => {
    mockLogin.mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderWithProviders(<LoginPage />);

    await user.type(screen.getByLabelText(/username/i), 'admin');
    await user.type(screen.getAllByLabelText(/password/i)[0], 'pass123');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(mockLogin).toHaveBeenCalledWith({ username: 'admin', password: 'pass123' });
  });

  it('shows error message on login failure', async () => {
    mockLogin.mockRejectedValue(new Error('Invalid credentials'));
    const user = userEvent.setup();

    renderWithProviders(<LoginPage />);

    await user.type(screen.getByLabelText(/username/i), 'wrong');
    await user.type(screen.getAllByLabelText(/password/i)[0], 'wrong');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText(/login failed/i)).toBeInTheDocument();
  });

  it('renders OIDC sign in when configured', () => {
    mockAuthConfig = {
      oidcEnabled: true,
      oidcLoginPath: '/api/auth/oidc/login',
    };

    renderWithProviders(<LoginPage />);

    expect(screen.getByRole('button', { name: /sign in with openid connect/i })).toBeInTheDocument();
  });

  it('starts OIDC sign in from the OIDC button', async () => {
    mockAuthConfig = {
      oidcEnabled: true,
      oidcLoginPath: '/api/auth/oidc/login',
    };
    const user = userEvent.setup();

    renderWithProviders(<LoginPage />);

    await user.click(screen.getByRole('button', { name: /sign in with openid connect/i }));

    expect(mockLoginWithOidc).toHaveBeenCalledTimes(1);
  });
});
