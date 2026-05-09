import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { SearchResult, BookmarkDto, UserPreferenceDto, CreateBookmarkRequest } from '../types';
import { useGlobalResourceSearch } from '../../../read-model/hooks';
import { syncSearchResults } from '../../../read-model/sync/resource-query-sync';

export function useSearch(query: string, resourceType?: string) {
  const localSearch = useGlobalResourceSearch({
    query,
    resourceType: resourceType ?? null,
    sortBy: 'relevance',
  });
  const serverSearch = useQuery({
    queryKey: ['search', query, resourceType],
    queryFn: async () => {
      const params: Record<string, string> = { q: query };
      if (resourceType) params.type = resourceType;
      const res = await apiClient.get<SearchResult[]>('/search', { params });
      syncSearchResults(res.data);
      return res.data;
    },
    enabled: query.length >= 2,
  });

  const localResults: SearchResult[] = localSearch.data.map(result => ({
    resourceType: result.resourceType,
    resourceId: result.resourceId,
    displayName: result.displayName,
    environmentId: result.environmentId,
    description: result.description ?? undefined,
  }));

  return {
    ...serverSearch,
    data: serverSearch.data ?? (localResults.length > 0 ? localResults : undefined),
    isFetching: serverSearch.isFetching || localSearch.isLoading,
  };
}

export function useBookmarks() {
  return useQuery({
    queryKey: ['bookmarks'],
    queryFn: async () => {
      const res = await apiClient.get<BookmarkDto[]>('/bookmarks');
      return res.data;
    },
  });
}

export function useCreateBookmark() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateBookmarkRequest) => {
      const res = await apiClient.post<BookmarkDto>('/bookmarks', data);
      return res.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bookmarks'] }),
  });
}

export function useDeleteBookmark() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/bookmarks/${id}`);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bookmarks'] }),
  });
}

export function usePreferences() {
  return useQuery({
    queryKey: ['preferences'],
    queryFn: async () => {
      const res = await apiClient.get<UserPreferenceDto[]>('/preferences');
      return res.data;
    },
  });
}

export function useSetPreference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ key, value }: { key: string; value: string }) => {
      await apiClient.put(`/preferences/${key}`, { value });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['preferences'] }),
  });
}
