import { useCallback, useEffect, useRef, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../api/client';
import type { NatsServerInfo, NatsSubjectInfo, PublishRequest, NatsLiveMessage } from '../types';

export function useCoreNatsStatus(environmentId: string | null) {
  return useQuery({
    queryKey: ['core-nats-status', environmentId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${environmentId}/core-nats/status`);
      return response.data as NatsServerInfo;
    },
    enabled: !!environmentId,
    refetchInterval: 15000,
  });
}

export interface UseSubjectsResult {
  data: NatsSubjectInfo[] | undefined;
  isLoading: boolean;
  error: Error | null;
  isMonitoringAvailable: boolean;
}

export function useSubjects(environmentId: string | null): UseSubjectsResult {
  const [isMonitoringAvailable, setIsMonitoringAvailable] = useState(true);

  const query = useQuery({
    queryKey: ['core-nats-subjects', environmentId],
    queryFn: async () => {
      const response = await apiClient.get<NatsSubjectInfo[]>(
        `/environments/${environmentId}/core-nats/subjects`,
        { validateStatus: () => true }
      );
      const source = response.headers['x-subjects-source'];
      setIsMonitoringAvailable(source !== 'unavailable');
      return response.data;
    },
    enabled: !!environmentId,
    refetchInterval: 15000,
  });

  return {
    data: query.data,
    isLoading: query.isLoading,
    error: query.error,
    isMonitoringAvailable,
  };
}

export function usePublishMessage(environmentId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: PublishRequest) => {
      await apiClient.post(`/environments/${environmentId}/core-nats/publish`, request);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['core-nats-status', environmentId] });
    },
  });
}

export interface UseLiveMessagesReturn {
  messages: NatsLiveMessage[];
  isConnected: boolean;
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
    setIsPaused(false);
    setPendingCount(0);
    pendingBufferRef.current = [];
  }, []);

  const subscribe = useCallback((subject: string) => {
    unsubscribe();
    if (!environmentId) return;

    const url = `/api/environments/${environmentId}/core-nats/stream?subject=${encodeURIComponent(subject)}`;
    const es = new EventSource(url);
    eventSourceRef.current = es;

    es.addEventListener('open', () => setIsConnected(true));
    es.addEventListener('error', () => setIsConnected(false));

    es.addEventListener('message', (event: MessageEvent) => {
      try {
        const msg = JSON.parse(event.data as string) as NatsLiveMessage;
        if (isPausedRef.current) {
          pendingBufferRef.current.unshift(msg);
          setPendingCount((c) => c + 1);
        } else {
          setMessages((prev) => [msg, ...prev].slice(0, capRef.current));
        }
      } catch {
        // ignore parse errors
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

  useEffect(() => {
    return () => {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, []);

  return {
    messages,
    isConnected,
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
