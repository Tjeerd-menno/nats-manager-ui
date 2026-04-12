import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { SearchResult, BookmarkDto, UserPreferenceDto, CreateBookmarkRequest } from '../types';

export function useSearch(query: string, resourceType?: string) {
  return useQuery({
    queryKey: ['search', query, resourceType],
    queryFn: async () => {
      const params: Record<string, string> = { q: query };
      if (resourceType) params.type = resourceType;
      const res = await apiClient.get<SearchResult[]>('/search', { params });
      return res.data;
    },
    enabled: query.length >= 2,
  });
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
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bookmarks'] });
    },
  });
}

export function useDeleteBookmark() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/bookmarks/${id}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bookmarks'] });
    },
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
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['preferences'] });
    },
  });
}
