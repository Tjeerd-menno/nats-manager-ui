import { QueryClient } from '@tanstack/react-query';
import { renderHook, waitFor, act } from '@testing-library/react';
import { createElement, type ReactNode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { usePublishMessage, useSubjects, useLiveMessages } from './useCoreNats';
import type { NatsLiveMessage } from '../types';

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

// ---------------------------------------------------------------------------
// Mock EventSource
// ---------------------------------------------------------------------------

class MockEventSource {
  static instances: MockEventSource[] = [];

  url: string;
  private listeners = new Map<string, ((e: Event | MessageEvent) => void)[]>();
  closed = false;

  constructor(url: string) {
    this.url = url;
    MockEventSource.instances.push(this);
  }

  addEventListener(type: string, handler: (e: Event | MessageEvent) => void) {
    if (!this.listeners.has(type)) this.listeners.set(type, []);
    this.listeners.get(type)!.push(handler);
  }

  emit(type: string, data?: NatsLiveMessage) {
    const handlers = this.listeners.get(type) ?? [];
    const event =
      type === 'message'
        ? new MessageEvent(type, { data: JSON.stringify(data) })
        : new Event(type);
    for (const h of handlers) h(event);
  }

  close() {
    this.closed = true;
  }

  static reset() {
    MockEventSource.instances = [];
  }
}

function makeMsg(subject = 'test'): NatsLiveMessage {
  return {
    subject,
    receivedAt: new Date().toISOString(),
    payloadBase64: btoa('hello'),
    payloadSize: 5,
    headers: {},
    isBinary: false,
  };
}

// ---------------------------------------------------------------------------
// usePublishMessage
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// useSubjects
// ---------------------------------------------------------------------------

describe('useSubjects', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('isMonitoringAvailable is true when x-subjects-source is "monitoring"', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: [{ subject: 'orders.>', subscriptions: 2 }],
      headers: { 'x-subjects-source': 'monitoring' },
    });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useSubjects('env-1'), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.data).toHaveLength(1);
    expect(result.current.isMonitoringAvailable).toBe(true);
  });

  it('isMonitoringAvailable is false when x-subjects-source is "unavailable"', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: [],
      headers: { 'x-subjects-source': 'unavailable' },
    });

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useSubjects('env-1'), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.isMonitoringAvailable).toBe(false);
  });

  it('surfaces errors so React Query can redirect on 401', async () => {
    const error = new Error('Unauthorized');
    vi.mocked(apiClient.get).mockRejectedValueOnce(error);

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useSubjects('env-1'), { wrapper });

    await waitFor(() => expect(result.current.error).not.toBeNull());

    expect(result.current.error).toBe(error);
  });
});

// ---------------------------------------------------------------------------
// useLiveMessages
// ---------------------------------------------------------------------------

describe('useLiveMessages', () => {
  beforeEach(() => {
    MockEventSource.reset();
    vi.stubGlobal('EventSource', MockEventSource);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('subscribe creates an EventSource with the correct URL', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useLiveMessages('env-1'), { wrapper });

    act(() => {
      result.current.subscribe('orders.>');
    });

    expect(MockEventSource.instances).toHaveLength(1);
    expect(MockEventSource.instances[0].url).toContain('/api/environments/env-1/core-nats/stream');
    expect(MockEventSource.instances[0].url).toContain('orders.%3E');
  });

  it('unsubscribe closes the EventSource and resets connection state', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useLiveMessages('env-1'), { wrapper });

    act(() => {
      result.current.subscribe('test.>');
    });
    const es = MockEventSource.instances[0];
    act(() => {
      es.emit('open');
    });
    expect(result.current.isConnected).toBe(true);

    act(() => {
      result.current.unsubscribe();
    });

    expect(es.closed).toBe(true);
    expect(result.current.isConnected).toBe(false);
  });

  it('messages are prepended and capped to cap value', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useLiveMessages('env-1'), { wrapper });

    act(() => {
      result.current.subscribe('test');
    });
    const es = MockEventSource.instances[0];

    act(() => {
      for (let i = 0; i < 120; i++) {
        es.emit('message', makeMsg(`s${i}`));
      }
    });

    expect(result.current.messages.length).toBe(100);
    // Most recent message is first
    expect(result.current.messages[0].subject).toBe('s119');
  });

  it('pause buffers incoming messages and resume flushes them', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useLiveMessages('env-1'), { wrapper });

    act(() => {
      result.current.subscribe('test');
    });
    const es = MockEventSource.instances[0];

    // Receive one message before pausing
    act(() => {
      es.emit('message', makeMsg('before'));
    });
    expect(result.current.messages.length).toBe(1);

    act(() => {
      result.current.pause();
    });

    act(() => {
      es.emit('message', makeMsg('during-1'));
      es.emit('message', makeMsg('during-2'));
    });

    expect(result.current.messages.length).toBe(1);
    expect(result.current.pendingCount).toBe(2);

    act(() => {
      result.current.resume();
    });

    expect(result.current.pendingCount).toBe(0);
    expect(result.current.messages.length).toBe(3);
  });

  it('pending buffer is capped when paused on a high-volume subject', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useLiveMessages('env-1'), { wrapper });

    act(() => {
      result.current.subscribe('test');
    });
    const es = MockEventSource.instances[0];

    act(() => {
      result.current.pause();
    });

    act(() => {
      for (let i = 0; i < 150; i++) {
        es.emit('message', makeMsg(`s${i}`));
      }
    });

    // pendingCount should be capped at cap (100)
    expect(result.current.pendingCount).toBeLessThanOrEqual(100);
  });

  it('clear empties the message list', () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useLiveMessages('env-1'), { wrapper });

    act(() => {
      result.current.subscribe('test');
    });
    const es = MockEventSource.instances[0];

    act(() => {
      es.emit('message', makeMsg());
      es.emit('message', makeMsg());
    });
    expect(result.current.messages.length).toBe(2);

    act(() => {
      result.current.clear();
    });

    expect(result.current.messages.length).toBe(0);
  });
});
