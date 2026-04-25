import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { LiveMessageViewer } from './LiveMessageViewer';
import type { NatsLiveMessage } from '../types';

const mockSubscribe = vi.fn();
const mockUnsubscribe = vi.fn();
const mockPause = vi.fn();
const mockResume = vi.fn();
const mockClear = vi.fn();
const mockSetCap = vi.fn();

const defaultState = {
  messages: [] as NatsLiveMessage[],
  isConnected: false,
  isPaused: false,
  pendingCount: 0,
  cap: 100,
  setCap: mockSetCap,
  subscribe: mockSubscribe,
  unsubscribe: mockUnsubscribe,
  pause: mockPause,
  resume: mockResume,
  clear: mockClear,
};

vi.mock('../hooks/useCoreNats', () => ({
  useLiveMessages: vi.fn(() => defaultState),
}));

import { useLiveMessages } from '../hooks/useCoreNats';
const mockUseLiveMessages = vi.mocked(useLiveMessages);

beforeEach(() => {
  vi.clearAllMocks();
  mockUseLiveMessages.mockReturnValue({ ...defaultState });
});

it('renders subscribe button and subject input', () => {
  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);
  expect(screen.getByRole('button', { name: /subscribe/i })).toBeInTheDocument();
  expect(screen.getByLabelText(/subject pattern/i)).toBeInTheDocument();
});

it('subscribe button triggers subscribe with subject', async () => {
  const user = userEvent.setup();
  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);

  const input = screen.getByLabelText(/subject pattern/i);
  await user.type(input, 'test.>');
  await user.click(screen.getByRole('button', { name: /subscribe/i }));

  expect(mockSubscribe).toHaveBeenCalledWith('test.>');
});

it('shows unsubscribe button when connected', () => {
  mockUseLiveMessages.mockReturnValue({ ...defaultState, isConnected: true });
  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);
  expect(screen.getByRole('button', { name: /unsubscribe/i })).toBeInTheDocument();
});

it('message appears in table', () => {
  const msg: NatsLiveMessage = {
    subject: 'orders.created',
    receivedAt: new Date().toISOString(),
    payloadBase64: btoa('{"id":"1"}'),
    payloadSize: 9,
    headers: {},
    isBinary: false,
  };
  mockUseLiveMessages.mockReturnValue({ ...defaultState, isConnected: true, messages: [msg] });

  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);

  expect(screen.getByText('orders.created')).toBeInTheDocument();
});

it('pause button calls pause when connected', async () => {
  const user = userEvent.setup();
  mockUseLiveMessages.mockReturnValue({ ...defaultState, isConnected: true });

  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);

  await user.click(screen.getByRole('button', { name: /pause/i }));
  expect(mockPause).toHaveBeenCalled();
});

it('resume button calls resume when paused', async () => {
  const user = userEvent.setup();
  mockUseLiveMessages.mockReturnValue({
    ...defaultState,
    isConnected: true,
    isPaused: true,
    pendingCount: 5,
  });

  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);

  await user.click(screen.getByRole('button', { name: /resume/i }));
  expect(mockResume).toHaveBeenCalled();
});

it('clear button empties the list', async () => {
  const user = userEvent.setup();
  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);

  await user.click(screen.getByRole('button', { name: /clear messages/i }));
  expect(mockClear).toHaveBeenCalled();
});

it('shows inline warning for subject pattern with spaces', async () => {
  const user = userEvent.setup();
  renderWithProviders(<LiveMessageViewer environmentId="env-1" />);

  const input = screen.getByLabelText(/subject pattern/i);
  await user.type(input, 'orders with spaces');

  expect(screen.getByText(/must not contain spaces/i)).toBeInTheDocument();
});
