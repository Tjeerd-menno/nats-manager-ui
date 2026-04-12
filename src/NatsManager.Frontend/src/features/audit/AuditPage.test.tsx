import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import AuditPage from './AuditPage';

vi.mock('./hooks/useAudit', () => ({
  useAuditEvents: vi.fn(),
}));

import { useAuditEvents } from './hooks/useAudit';
const mockUseAuditEvents = vi.mocked(useAuditEvents);

describe('AuditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    mockUseAuditEvents.mockReturnValue({
      data: undefined,
      isLoading: true,
    } as unknown as ReturnType<typeof useAuditEvents>);

    const { container } = renderWithProviders(<AuditPage />);
    expect(container.querySelector('.mantine-Loader-root')).toBeInTheDocument();
  });

  it('renders audit event list', () => {
    mockUseAuditEvents.mockReturnValue({
      data: {
        items: [
          {
            id: 'ae-1',
            actionType: 'CreateStream',
            resourceType: 'Stream',
            resourceId: 'orders',
            performedBy: 'admin',
            timestamp: new Date().toISOString(),
            details: '{}',
            outcome: 'Success',
            actorId: 'u-1',
            actorName: 'admin',
          },
        ],
        totalPages: 1,
        totalCount: 1,
        page: 1,
        pageSize: 50,
      },
      isLoading: false,
    } as unknown as ReturnType<typeof useAuditEvents>);

    renderWithProviders(<AuditPage />);
    expect(screen.getByText('Audit Log')).toBeInTheDocument();
    expect(screen.getByText('CreateStream')).toBeInTheDocument();
  });

  it('renders empty state when no events', () => {
    mockUseAuditEvents.mockReturnValue({
      data: { items: [], totalPages: 0, totalCount: 0, page: 1, pageSize: 50 },
      isLoading: false,
    } as unknown as ReturnType<typeof useAuditEvents>);

    renderWithProviders(<AuditPage />);
    expect(screen.getByText('Audit Log')).toBeInTheDocument();
  });
});
