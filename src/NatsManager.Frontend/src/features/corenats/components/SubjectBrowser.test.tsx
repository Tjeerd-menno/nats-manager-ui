import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { SubjectBrowser } from './SubjectBrowser';

vi.mock('../hooks/useCoreNats', () => ({
  useSubjects: vi.fn(),
}));

import { useSubjects } from '../hooks/useCoreNats';
const mockUseSubjects = vi.mocked(useSubjects);

beforeEach(() => {
  vi.clearAllMocks();
});

it('renders subject table with data', () => {
  mockUseSubjects.mockReturnValue({
    data: [
      { subject: 'orders.>', subscriptions: 3 },
      { subject: 'events.created', subscriptions: 1 },
    ],
    isLoading: false,
    error: null,
    isMonitoringAvailable: true,
  });

  renderWithProviders(<SubjectBrowser environmentId="env-1" />);

  expect(screen.getByText('orders.>')).toBeInTheDocument();
  expect(screen.getByText('events.created')).toBeInTheDocument();
  expect(screen.getByText('3')).toBeInTheDocument();
});

it('filter reduces visible rows', async () => {
  const user = userEvent.setup();

  mockUseSubjects.mockReturnValue({
    data: [
      { subject: 'orders.>', subscriptions: 3 },
      { subject: 'events.created', subscriptions: 1 },
    ],
    isLoading: false,
    error: null,
    isMonitoringAvailable: true,
  });

  const { unmount } = renderWithProviders(<SubjectBrowser environmentId="env-1" />);

  const filter = screen.getByRole('textbox', { name: /filter subjects/i });
  await user.type(filter, 'orders');

  // Wait for the 300ms debounce to fire and the component to re-render
  await waitFor(() => {
    expect(screen.queryByText('events.created')).not.toBeInTheDocument();
  }, { timeout: 1000 });

  unmount();
}, 3000);

it('shows unavailable placeholder when monitoring not available', () => {
  mockUseSubjects.mockReturnValue({
    data: [],
    isLoading: false,
    error: null,
    isMonitoringAvailable: false,
  });

  renderWithProviders(<SubjectBrowser environmentId="env-1" />);

  expect(screen.getByText(/monitoring endpoint not reachable/i)).toBeInTheDocument();
});

it('shows no active subscriptions empty state when no subjects', () => {
  mockUseSubjects.mockReturnValue({
    data: [],
    isLoading: false,
    error: null,
    isMonitoringAvailable: true,
  });

  renderWithProviders(<SubjectBrowser environmentId="env-1" />);

  expect(screen.getByText(/no active subscriptions/i)).toBeInTheDocument();
});
