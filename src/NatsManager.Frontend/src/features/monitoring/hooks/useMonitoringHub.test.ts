import { renderHook, waitFor } from '@testing-library/react';
import { useMonitoringHub } from './useMonitoringHub';

const signalRMocks = vi.hoisted(() => ({
  builder: vi.fn(),
  start: vi.fn(),
  stop: vi.fn(),
  invoke: vi.fn(),
  on: vi.fn(),
  onReconnecting: vi.fn(),
  onReconnected: vi.fn(),
  onClose: vi.fn(),
  withUrl: vi.fn(),
  withAutomaticReconnect: vi.fn(),
}));

vi.mock('../../../api/client', () => ({
  apiClient: {
    get: vi.fn(),
  },
}));

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    constructor() {
      signalRMocks.builder();
    }

    withUrl(...args: unknown[]) {
      signalRMocks.withUrl(...args);
      return this;
    }

    withAutomaticReconnect() {
      signalRMocks.withAutomaticReconnect();
      return this;
    }

    build() {
      return {
        start: signalRMocks.start,
        stop: signalRMocks.stop,
        invoke: signalRMocks.invoke,
        on: signalRMocks.on,
        onreconnecting: signalRMocks.onReconnecting,
        onreconnected: signalRMocks.onReconnected,
        onclose: signalRMocks.onClose,
      };
    }
  },
}));

import { apiClient } from '../../../api/client';

describe('useMonitoringHub', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    signalRMocks.start.mockResolvedValue(undefined);
    signalRMocks.stop.mockResolvedValue(undefined);
    signalRMocks.invoke.mockResolvedValue(undefined);
  });

  it('returns not configured and does not start SignalR when history returns 400', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 400 },
    });

    const { result } = renderHook(() => useMonitoringHub('env-1'));

    await waitFor(() => expect(result.current.isNotConfigured).toBe(true));

    expect(result.current.connectionStatus).toBe('disconnected');
    expect(result.current.snapshots).toEqual([]);
    expect(signalRMocks.builder).not.toHaveBeenCalled();
  });

  it('loads history and subscribes to monitoring hub', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: {
        environmentId: 'env-1',
        snapshots: [
          { environmentId: 'env-1', timestamp: '2026-01-01T00:00:00Z', server: server(), jetStream: null, status: 'Ok', healthStatus: 'Ok' },
        ],
      },
    });

    const { result } = renderHook(() => useMonitoringHub('env-1'));

    await waitFor(() => expect(result.current.connectionStatus).toBe('connected'));

    expect(result.current.latestSnapshot?.environmentId).toBe('env-1');
    expect(signalRMocks.start).toHaveBeenCalled();
    expect(signalRMocks.invoke).toHaveBeenCalledWith('SubscribeToEnvironment', 'env-1');
  });
});

function server() {
  return {
    version: '2.10.0',
    connections: 1,
    totalConnections: 1,
    maxConnections: 100,
    inMsgsTotal: 0,
    outMsgsTotal: 0,
    inBytesTotal: 0,
    outBytesTotal: 0,
    inMsgsPerSec: 0,
    outMsgsPerSec: 0,
    inBytesPerSec: 0,
    outBytesPerSec: 0,
    uptimeSeconds: 0,
    memoryBytes: 0,
  };
}
