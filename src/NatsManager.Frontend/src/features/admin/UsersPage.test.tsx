import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../test-utils';
import UsersPage from './UsersPage';
import { useCreateUser } from './hooks/useAdmin';

vi.mock('./hooks/useAdmin', () => ({
  useUsers: vi.fn(),
  useRoles: vi.fn(() => ({ data: [] })),
  useUserRoles: vi.fn(() => ({ data: [] })),
  useCreateUser: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useDeactivateUser: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useAssignRole: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useRevokeRole: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}));

import { useUsers } from './hooks/useAdmin';
const mockUseUsers = vi.mocked(useUsers);

describe('UsersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    mockUseUsers.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as ReturnType<typeof useUsers>);

    const { container } = renderWithProviders(<UsersPage />);
    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('renders user list with data', () => {
    mockUseUsers.mockReturnValue({
      data: [
        { id: 'u-1', username: 'admin', displayName: 'Admin User', isActive: true, lastLoginAt: null, createdAt: new Date().toISOString() },
        { id: 'u-2', username: 'viewer', displayName: 'View User', isActive: false, lastLoginAt: null, createdAt: new Date().toISOString() },
      ],
      isLoading: false,
    } as unknown as ReturnType<typeof useUsers>);

    renderWithProviders(<UsersPage />);
    expect(screen.getByText('admin')).toBeInTheDocument();
    expect(screen.getByText('viewer')).toBeInTheDocument();
    expect(screen.getByText('Create User')).toBeInTheDocument();
  });

  it('shows Active/Inactive badges', () => {
    mockUseUsers.mockReturnValue({
      data: [
        { id: 'u-1', username: 'admin', displayName: 'Admin', isActive: true, lastLoginAt: null, createdAt: new Date().toISOString() },
        { id: 'u-2', username: 'viewer', displayName: 'Viewer', isActive: false, lastLoginAt: null, createdAt: new Date().toISOString() },
      ],
      isLoading: false,
    } as unknown as ReturnType<typeof useUsers>);

    renderWithProviders(<UsersPage />);
    expect(screen.getByText('Active')).toBeInTheDocument();
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('validates user creation input before submitting', async () => {
    const user = userEvent.setup();
    const createUser = vi.fn();
    vi.mocked(useCreateUser).mockReturnValue({
      mutate: createUser,
      isPending: false,
    } as unknown as ReturnType<typeof useCreateUser>);
    mockUseUsers.mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as ReturnType<typeof useUsers>);

    renderWithProviders(<UsersPage />);

    await user.click(screen.getByRole('button', { name: 'Create User' }));
    await waitFor(() => expect(document.querySelector('.mantine-Modal-content')).toBeInTheDocument());
    const textInputs = screen.getAllByRole('textbox');
    const passwordInput = document.querySelector('input[type="password"]');

    expect(passwordInput).toBeInstanceOf(HTMLInputElement);
    await user.type(textInputs[0], 'operator');
    await user.type(textInputs[1], 'Operator');
    await user.type(passwordInput as HTMLInputElement, 'short');
    await user.click(screen.getByRole('button', { name: 'Create' }));

    expect(await screen.findByText('Password must be at least 12 characters long')).toBeInTheDocument();
    expect(createUser).not.toHaveBeenCalled();
  });
});
