import { QueryClient } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { createElement, type ReactNode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { usePublishMessage } from './useCoreNats';

vi.mock('../../../api/client', () => ({
  apiClient: {
    post: vi.fn(() => Promise.resolve({ data: { published: true } })),
    get: vi.fn(),
  },
}));

import { apiClient } from '../../../api/client';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) =>
      createElement(QueryClientProvider, { client: queryClient }, children),
  };
}

describe('usePublishMessage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('invalidates core-nats-status query on successful publish', async () => {
    const { queryClient, wrapper } = createWrapper();

    // Seed the status query so we can detect invalidation
    queryClient.setQueryData(['core-nats-status', 'env-1'], { inMsgs: 0 });
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => usePublishMessage('env-1'), { wrapper });

    result.current.mutate({ subject: 'test.subject', payload: 'hello' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(apiClient.post).toHaveBeenCalledWith('/environments/env-1/core-nats/publish', {
      subject: 'test.subject',
      payload: 'hello',
    });

    expect(invalidateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: ['core-nats-status', 'env-1'],
      }),
    );
  });

  it('does not invalidate query on failed publish', async () => {
    vi.mocked(apiClient.post).mockRejectedValueOnce(new Error('Network error'));

    const { queryClient, wrapper } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => usePublishMessage('env-1'), { wrapper });

    result.current.mutate({ subject: 'test.subject' });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(invalidateSpy).not.toHaveBeenCalled();
  });
});
