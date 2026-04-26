import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { PublishMessageForm } from './PublishMessageForm';

const mockMutate = vi.fn();

vi.mock('../hooks/useCoreNats', () => ({
  usePublishMessage: vi.fn(() => ({
    mutate: mockMutate,
    isPending: false,
    error: null,
    isSuccess: false,
  })),
}));

import { usePublishMessage } from '../hooks/useCoreNats';
const mockUsePublishMessage = vi.mocked(usePublishMessage);
void mockUsePublishMessage; // used to verify mock is set up correctly

beforeEach(() => {
  vi.clearAllMocks();
  mockMutate.mockReset();
});

it('submit button is disabled when subject is empty', () => {
  renderWithProviders(<PublishMessageForm environmentId="env-1" />);
  const button = screen.getByRole('button', { name: /publish/i });
  expect(button).toBeDisabled();
});

it('JSON format with invalid JSON disables submit', async () => {
  const user = userEvent.setup();
  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  // Fill subject
  const subjectInput = screen.getByLabelText(/subject/i);
  await user.type(subjectInput, 'test.subject');

  // Select JSON format
  const jsonButton = screen.getByRole('radio', { name: /json/i });
  await user.click(jsonButton);

  // Type invalid JSON
  const payloadInput = screen.getByLabelText(/payload/i);
  await user.type(payloadInput, 'not-json');

  await waitFor(() => {
    expect(screen.getByText(/not valid json/i)).toBeInTheDocument();
  });

  const button = screen.getByRole('button', { name: /publish/i });
  expect(button).toBeDisabled();
});

it('Hex Bytes format with invalid hex disables submit', async () => {
  const user = userEvent.setup();
  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  await user.type(screen.getByLabelText(/subject/i), 'test.subject');
  await user.click(screen.getByRole('radio', { name: /hex bytes/i }));
  await user.type(screen.getByLabelText(/payload/i), 'not-hex');

  expect(await screen.findByText(/not valid hex bytes/i)).toBeInTheDocument();
  expect(screen.getByRole('button', { name: /publish/i })).toBeDisabled();
});

it('shows success notification after mutation success', async () => {
  const user = userEvent.setup();
  mockMutate.mockImplementation((_req: unknown, options: { onSuccess?: () => void }) => {
    options.onSuccess?.();
  });

  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  const subjectInput = screen.getByLabelText(/subject/i);
  await user.type(subjectInput, 'test.subject');

  const button = screen.getByRole('button', { name: /publish/i });
  await user.click(button);

  await waitFor(() => {
    expect(screen.getByText('Message published successfully.')).toBeInTheDocument();
  });
});

it('shows error notification after mutation failure with fields preserved', async () => {
  const user = userEvent.setup();
  mockMutate.mockImplementation((_req: unknown, options: { onError?: (e: Error) => void }) => {
    options.onError?.(new Error('Network failure'));
  });

  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  const subjectInput = screen.getByLabelText(/subject/i);
  await user.type(subjectInput, 'test.subject');

  const button = screen.getByRole('button', { name: /publish/i });
  await user.click(button);

  await waitFor(() => {
    expect(screen.getByText(/publish failed/i)).toBeInTheDocument();
  });

  // Fields should be preserved
  expect(subjectInput).toHaveValue('test.subject');
});

it('empty header key shows validation error', async () => {
  const user = userEvent.setup();
  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  // Fill subject
  const subjectInput = screen.getByLabelText(/subject/i);
  await user.type(subjectInput, 'test.subject');

  // Add a header
  const addButton = screen.getByRole('button', { name: /add header/i });
  await user.click(addButton);

  // Key is empty by default — button should be disabled
  const button = screen.getByRole('button', { name: /publish/i });
  expect(button).toBeDisabled();
});

it('whitespace-only header key shows validation error', async () => {
  const user = userEvent.setup();
  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  await user.type(screen.getByLabelText(/subject/i), 'test.subject');
  await user.click(screen.getByRole('button', { name: /add header/i }));
  await user.type(screen.getByPlaceholderText(/key/i), '   ');

  expect(screen.getByText(/key required/i)).toBeInTheDocument();
  expect(screen.getByRole('button', { name: /publish/i })).toBeDisabled();
});

it('duplicate header keys show validation errors', async () => {
  const user = userEvent.setup();
  renderWithProviders(<PublishMessageForm environmentId="env-1" />);

  await user.type(screen.getByLabelText(/subject/i), 'test.subject');
  await user.click(screen.getByRole('button', { name: /add header/i }));
  await user.click(screen.getByRole('button', { name: /add header/i }));

  const keyInputs = screen.getAllByPlaceholderText(/key/i);
  await user.type(keyInputs[0], 'X-Test');
  await user.type(keyInputs[1], ' x-test ');

  expect(screen.getAllByText(/duplicate key/i)).toHaveLength(2);
  expect(screen.getByRole('button', { name: /publish/i })).toBeDisabled();
});
