import { useMemo } from 'react';
import type { ServerStatus } from '../types';
import { useClusterOverview } from './useClusterOverview';

const STATUS_ORDER: Record<ServerStatus, number> = {
  Healthy: 0,
  Warning: 1,
  Stale: 2,
  Unavailable: 3,
  Unknown: 4,
};

interface UseClusterServersOptions {
  search?: string;
}

export function useClusterServers(envId: string, { search = '' }: UseClusterServersOptions = {}) {
  const { data, isLoading, isError, error } = useClusterOverview(envId);

  const servers = useMemo(() => {
    const all = data?.servers ?? [];
    const term = search.trim().toLowerCase();
    const filtered = term
      ? all.filter(
          s =>
            s.serverId.toLowerCase().includes(term) ||
            (s.serverName ?? '').toLowerCase().includes(term),
        )
      : all;

    return [...filtered].sort((a, b) => {
      const statusDiff = STATUS_ORDER[a.status] - STATUS_ORDER[b.status];
      if (statusDiff !== 0) return statusDiff;
      return a.serverId.localeCompare(b.serverId);
    });
  }, [data?.servers, search]);

  return { servers, isLoading, isError, error };
}
