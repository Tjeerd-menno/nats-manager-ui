import { describe, expect, it, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useReconnectStatus } from './useReconnectStatus';

describe('useReconnectStatus', () => {
  it('starts idle and updates to reconnecting when reconnect begins', async () => {
    const subscribe = vi.fn();
    const unsubscribe = vi.fn();

    const connection = {
      subscribe,
      unsubscribe,
    };

    const { result } = renderHook(() => useReconnectStatus(connection));

    expect(result.current.status).toBe('idle');

    const onReconnect = subscribe.mock.calls.find(([eventName]) => eventName === 'reconnecting')?.[1];
    expect(onReconnect).toBeTypeOf('function');

    act(() => {
      onReconnect?.();
    });

    await waitFor(() => {
      expect(result.current.status).toBe('reconnecting');
    });
  });

  it('updates to connected after a successful reconnect', async () => {
    const subscribe = vi.fn();
    const unsubscribe = vi.fn();

    const connection = {
      subscribe,
      unsubscribe,
    };

    const { result } = renderHook(() => useReconnectStatus(connection));

    const onReconnected = subscribe.mock.calls.find(([eventName]) => eventName === 'reconnected')?.[1];
    expect(onReconnected).toBeTypeOf('function');

    act(() => {
      onReconnected?.();
    });

    await waitFor(() => {
      expect(result.current.status).toBe('connected');
    });
  });

  it('cleans up subscriptions on unmount', () => {
    const subscribe = vi.fn();
    const unsubscribe = vi.fn();

    const connection = {
      subscribe,
      unsubscribe,
    };

    const { unmount } = renderHook(() => useReconnectStatus(connection));

    unmount();

    expect(unsubscribe).toHaveBeenCalled();
  });
});