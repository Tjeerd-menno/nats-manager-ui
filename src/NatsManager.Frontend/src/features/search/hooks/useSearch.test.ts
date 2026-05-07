import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { createElement } from 'react';
import type { ReactNode } from 'react';
import { apiClient } from '../../../api/client';
import { useBookmarks, useCreateBookmark, useDeleteBookmark, usePreferences, useSearch, useSetPreference } from './useSearch';

vi.mock('../../../api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
    put: vi.fn(),
  },
}));

const mockApiClient = vi.mocked(apiClient);

function createWrapper(queryClient: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

describe('useSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('does not request results until the query has at least two characters', () => {
    const queryClient = createQueryClient();

    const { result } = renderHook(() => useSearch('a'), {
      wrapper: createWrapper(queryClient),
    });

    expect(result.current.fetchStatus).toBe('idle');
    expect(mockApiClient.get).not.toHaveBeenCalled();
  });

  it('passes the resource type filter to the search endpoint', async () => {
    const queryClient = createQueryClient();
    mockApiClient.get.mockResolvedValue({ data: [{ resourceType: 'Stream', resourceId: 'orders', displayName: 'Orders' }] });

    const { result } = renderHook(() => useSearch('orders', 'Stream'), {
      wrapper: createWrapper(queryClient),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockApiClient.get).toHaveBeenCalledWith('/search', {
      params: { q: 'orders', type: 'Stream' },
    });
  });
});

describe('bookmark and preference hooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('loads bookmarks from the bookmarks endpoint', async () => {
    const queryClient = createQueryClient();
    mockApiClient.get.mockResolvedValue({ data: [{ id: 'bookmark-1' }] });

    const { result } = renderHook(() => useBookmarks(), {
      wrapper: createWrapper(queryClient),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockApiClient.get).toHaveBeenCalledWith('/bookmarks');
    expect(result.current.data).toEqual([{ id: 'bookmark-1' }]);
  });

  it('invalidates bookmark queries after creating a bookmark', async () => {
    const queryClient = createQueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    mockApiClient.post.mockResolvedValue({ data: { id: 'bookmark-1' } });

    const { result } = renderHook(() => useCreateBookmark(), {
      wrapper: createWrapper(queryClient),
    });

    result.current.mutate({
      environmentId: 'env-1',
      resourceType: 'Stream',
      resourceId: 'orders',
      displayName: 'Orders',
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockApiClient.post).toHaveBeenCalledWith('/bookmarks', {
      environmentId: 'env-1',
      resourceType: 'Stream',
      resourceId: 'orders',
      displayName: 'Orders',
    });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['bookmarks'] });
  });

  it('invalidates bookmark queries after deleting a bookmark', async () => {
    const queryClient = createQueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    mockApiClient.delete.mockResolvedValue({ data: undefined });

    const { result } = renderHook(() => useDeleteBookmark(), {
      wrapper: createWrapper(queryClient),
    });

    result.current.mutate('bookmark-1');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockApiClient.delete).toHaveBeenCalledWith('/bookmarks/bookmark-1');
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['bookmarks'] });
  });

  it('loads preferences from the preferences endpoint', async () => {
    const queryClient = createQueryClient();
    mockApiClient.get.mockResolvedValue({ data: [{ key: 'theme', value: 'dark' }] });

    const { result } = renderHook(() => usePreferences(), {
      wrapper: createWrapper(queryClient),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockApiClient.get).toHaveBeenCalledWith('/preferences');
    expect(result.current.data).toEqual([{ key: 'theme', value: 'dark' }]);
  });

  it('invalidates preferences after updating one', async () => {
    const queryClient = createQueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    mockApiClient.put.mockResolvedValue({ data: undefined });

    const { result } = renderHook(() => useSetPreference(), {
      wrapper: createWrapper(queryClient),
    });

    result.current.mutate({ key: 'theme', value: 'dark' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(mockApiClient.put).toHaveBeenCalledWith('/preferences/theme', { value: 'dark' });
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['preferences'] });
  });
});
