import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import UsersPage from './UsersPage';

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
});
