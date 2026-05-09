import { useMemo } from 'react';
import { useLiveQuery } from '@tanstack/react-db';
import { resourceIndexCollection } from '../collections';
import type { GlobalResourceSearchOptions } from '../types/resource-index-read-model';

const MIN_QUERY_LENGTH = 2;

export function useGlobalResourceSearch(options: GlobalResourceSearchOptions) {
  const { data = [], ...query } = useLiveQuery((q) => q.from({ resource: resourceIndexCollection }), []);
  const normalizedQuery = options.query.trim().toLowerCase();

  const results = useMemo(() => {
    if (normalizedQuery.length < MIN_QUERY_LENGTH) {
      return [];
    }

    const filtered = data.filter(resource => {
      if (options.environmentId && resource.environmentId !== options.environmentId) return false;
      if (options.resourceType && resource.resourceType !== options.resourceType) return false;
      if (options.status && resource.status !== options.status) return false;
      if (options.freshness && resource.freshness !== options.freshness) return false;
      if (normalizedQuery && !resource.searchableText.includes(normalizedQuery)) return false;
      return true;
    });

    return [...filtered].sort((left, right) => {
      if (options.sortBy === 'resourceType') return left.resourceType.localeCompare(right.resourceType) || left.displayName.localeCompare(right.displayName);
      if (options.sortBy === 'name') return left.displayName.localeCompare(right.displayName);
      return relevance(right.searchableText, normalizedQuery) - relevance(left.searchableText, normalizedQuery)
        || left.displayName.localeCompare(right.displayName);
    });
  }, [data, normalizedQuery, options.environmentId, options.freshness, options.resourceType, options.sortBy, options.status]);

  return { ...query, data: results };
}

function relevance(text: string, query: string) {
  if (!query) return 0;
  if (text.startsWith(query)) return 3;
  if (text.includes(` ${query}`)) return 2;
  return text.includes(query) ? 1 : 0;
}
