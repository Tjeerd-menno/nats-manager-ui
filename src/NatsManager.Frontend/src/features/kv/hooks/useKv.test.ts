import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { createElement } from 'react';
import type { ReactNode } from 'react';
import { apiClient } from '../../../api/client';
import { queryKeys } from '../../../api/queryKeys';
import { usePutKvKey } from './useKv';

vi.mock('../../../api/client', () => ({
  apiClient: {
    put: vi.fn(),
  },
}));

const mockApiClient = vi.mocked(apiClient);

function createWrapper(queryClient: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('usePutKvKey', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiClient.put.mockResolvedValue({ data: { revision: 1 } });
  });

  it('invalidates bucket summaries after writing a key', async () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => usePutKvKey('env-1', 'qa-bucket'), {
      wrapper: createWrapper(queryClient),
    });

    result.current.mutate({ key: 'hello', value: 'world' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.kvBucket('env-1', 'qa-bucket') });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: queryKeys.kvBuckets('env-1') });
  });
});
