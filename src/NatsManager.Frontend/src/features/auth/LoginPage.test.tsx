import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import { LoginPage } from './LoginPage';

const mockLogin = vi.fn();

vi.mock('./useAuth', () => ({
  useAuth: vi.fn(() => ({
    login: mockLogin,
    logout: vi.fn(),
    user: null,
    isAuthenticated: false,
    isLoading: false,
    hasRole: vi.fn(() => false),
  })),
}));

vi.mock('../../api/client', () => ({
  extractProblemDetails: vi.fn(() => null),
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
});
