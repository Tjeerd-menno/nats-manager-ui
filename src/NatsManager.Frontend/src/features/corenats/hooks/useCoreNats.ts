import { useCallback, useEffect, useRef, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import { apiEndpoints, toApiUrl } from '../../../api/endpoints';
import { pollingIntervals } from '../../../api/queryConfig';
import { queryKeys } from '../../../api/queryKeys';
import type { NatsServerInfo, NatsSubjectInfo, PublishRequest, NatsLiveMessage } from '../types';

export function useCoreNatsStatus(environmentId: string | null) {
  return useQuery({
    queryKey: queryKeys.coreNatsStatus(environmentId),
    queryFn: async () => {
      const response = await apiClient.get(apiEndpoints.coreNatsStatus(environmentId));
      return response.data as NatsServerInfo;
    },
    enabled: !!environmentId,
    refetchInterval: pollingIntervals.coreNats,
  });
}

export interface UseSubjectsResult {
  data: NatsSubjectInfo[] | undefined;
  isLoading: boolean;
  error: Error | null;
  isMonitoringAvailable: boolean;
}

export function useSubjects(environmentId: string | null): UseSubjectsResult {
  const query = useQuery({
    queryKey: queryKeys.coreNatsSubjects(environmentId),
    queryFn: async () => {
      const response = await apiClient.get<NatsSubjectInfo[]>(apiEndpoints.coreNatsSubjects(environmentId));
      const source = response.headers['x-subjects-source'];
      return {
        data: response.data,
        isMonitoringAvailable: source !== 'unavailable',
      };
    },
    enabled: !!environmentId,
    refetchInterval: pollingIntervals.coreNats,
  });

  return {
    data: query.data?.data,
    isLoading: query.isLoading,
    error: query.error,
    isMonitoringAvailable: query.data?.isMonitoringAvailable ?? true,
  };
}

export function usePublishMessage(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: PublishRequest) => {
      await apiClient.post(apiEndpoints.coreNatsPublish(environmentId), request);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.coreNatsStatus(environmentId) });
    },
  });
}

export interface UseLiveMessagesReturn {
  messages: NatsLiveMessage[];
  isConnected: boolean;
  subscriptionStatus: 'idle' | 'connecting' | 'connected' | 'reconnecting';
  activeSubject: string | null;
  isPaused: boolean;
  pendingCount: number;
  cap: number;
  setCap: (cap: number) => void;
  subscribe: (subject: string) => void;
  unsubscribe: () => void;
  pause: () => void;
  resume: () => void;
  clear: () => void;
}

export function useLiveMessages(environmentId: string | null): UseLiveMessagesReturn {
  const [messages, setMessages] = useState<NatsLiveMessage[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [subscriptionStatus, setSubscriptionStatus] = useState<'idle' | 'connecting' | 'connected' | 'reconnecting'>('idle');
  const [activeSubject, setActiveSubject] = useState<string | null>(null);
  const [isPaused, setIsPaused] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [cap, setCap] = useState(100);

  const eventSourceRef = useRef<EventSource | null>(null);
  const pendingBufferRef = useRef<NatsLiveMessage[]>([]);
  const isPausedRef = useRef(false);
  const capRef = useRef(100);

  useEffect(() => {
    isPausedRef.current = isPaused;
  }, [isPaused]);

  useEffect(() => {
    capRef.current = cap;
  }, [cap]);

  const unsubscribe = useCallback(() => {
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
    }
    setIsConnected(false);
    setSubscriptionStatus('idle');
    setActiveSubject(null);
    setIsPaused(false);
    setPendingCount(0);
    pendingBufferRef.current = [];
  }, []);

  const subscribe = useCallback((subject: string) => {
    unsubscribe();
    if (!environmentId) return;

    setActiveSubject(subject);
    setSubscriptionStatus('connecting');

    const es = new EventSource(toApiUrl(apiEndpoints.coreNatsStream(environmentId, subject)));
    eventSourceRef.current = es;

    es.addEventListener('open', () => {
      setIsConnected(true);
      setSubscriptionStatus('connected');
    });
    es.addEventListener('error', () => {
      setIsConnected(false);
      setSubscriptionStatus('reconnecting');
    });

    es.addEventListener('message', (event: MessageEvent) => {
      try {
        const msg = JSON.parse(event.data as string) as NatsLiveMessage;
        if (isPausedRef.current) {
          pendingBufferRef.current.unshift(msg);
          if (pendingBufferRef.current.length > capRef.current) {
            pendingBufferRef.current = pendingBufferRef.current.slice(0, capRef.current);
          }
          setPendingCount(pendingBufferRef.current.length);
        } else {
          setMessages((prev) => [msg, ...prev].slice(0, capRef.current));
        }
      } catch {
        return;
      }
    });
  }, [environmentId, unsubscribe]);

  const pause = useCallback(() => setIsPaused(true), []);

  const resume = useCallback(() => {
    setIsPaused(false);
    const pending = pendingBufferRef.current;
    pendingBufferRef.current = [];
    setPendingCount(0);
    if (pending.length > 0) {
      setMessages((prev) => [...pending, ...prev].slice(0, capRef.current));
    }
  }, []);

  const clear = useCallback(() => {
    setMessages([]);
    pendingBufferRef.current = [];
    setPendingCount(0);
  }, []);

  const updateCap = useCallback((newCap: number) => {
    const clamped = Math.max(100, Math.min(500, newCap));
    setCap(clamped);
    setMessages((prev) => prev.slice(0, clamped));
  }, []);

  useEffect(() => unsubscribe, [unsubscribe]);

  return {
    messages,
    isConnected,
    subscriptionStatus,
    activeSubject,
    isPaused,
    pendingCount,
    cap,
    setCap: updateCap,
    subscribe,
    unsubscribe,
    pause,
    resume,
    clear,
  };
}
