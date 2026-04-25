# API Contracts: Live Environment Monitoring

**Branch**: `copilot/add-live-monitoring-feature`  
**Date**: 2026-04-25

---

## 1. REST Endpoints

### 1.1 Get Monitoring History

Returns the in-memory snapshot history for an environment (used on initial page load before SignalR updates begin).

```
GET /api/environments/{envId:guid}/monitoring/metrics/history
```

**Authorization**: Requires authenticated session (`RequireAuthorization`)

**Response `200 OK`**:
```json
{
  "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "snapshots": [
    {
      "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "timestamp": "2026-04-25T13:00:00Z",
      "server": {
        "version": "2.10.18",
        "connections": 42,
        "totalConnections": 1500,
        "maxConnections": 65536,
        "inMsgsPerSec": 125.3,
        "outMsgsPerSec": 118.7,
        "inBytesPerSec": 65536.0,
        "outBytesPerSec": 32768.0,
        "uptimeSeconds": 86400,
        "memoryBytes": 12582912
      },
      "jetStream": {
        "streamCount": 5,
        "consumerCount": 12,
        "totalMessages": 50000,
        "totalBytes": 2097152
      },
      "status": "Ok"
    }
  ]
}
```

**Response `404 Not Found`**: Environment not found.  
**Response `400 Bad Request`**: Monitoring not configured for this environment (`MonitoringUrl` is null).

---

### 1.2 Update Environment Monitoring Settings

Extends the existing `PUT /api/environments/{envId}` endpoint with two new optional fields.

```
PUT /api/environments/{envId:guid}
```

**Request body (extended with new fields)**:
```json
{
  "name": "production",
  "serverUrl": "nats://prod-server:4222",
  "description": "Production cluster",
  "credentialType": "Token",
  "credentialReference": null,
  "isProduction": true,
  "monitoringUrl": "http://prod-server:8222",
  "monitoringPollingIntervalSeconds": 15
}
```

New fields:
- `monitoringUrl` (string | null): HTTP URL of NATS monitoring API. Set to `null` to disable monitoring.
- `monitoringPollingIntervalSeconds` (integer | null): Per-environment polling interval. Set to `null` to use global default.

**Validation errors `422 Unprocessable Entity`**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "errors": {
    "monitoringUrl": ["MonitoringUrl must be a valid http:// or https:// URL"],
    "monitoringPollingIntervalSeconds": ["MonitoringPollingIntervalSeconds must be between 5 and 300"]
  }
}
```

---

## 2. SignalR Hub Contract

### Hub Route

```
/hubs/monitoring
```

**Transport**: WebSocket (primary), Server-Sent Events (fallback), Long-Polling (final fallback)  
**Authentication**: Cookie-based session (same as REST API; `withCredentials: true` on JS client)

---

### 2.1 Client → Server Methods

#### `SubscribeToEnvironment`

Joins the environment-specific group to receive metric pushes for the specified environment.

```typescript
connection.invoke('SubscribeToEnvironment', environmentId: string): Promise<void>
```

**Parameters**:
- `environmentId` (string): GUID of the environment to subscribe to.

**Behavior**: The hub adds the connection to the `env-{environmentId}` group. Subsequent `ReceiveMonitoringSnapshot` messages for that environment are delivered to this connection.

#### `UnsubscribeFromEnvironment`

Leaves the environment-specific group.

```typescript
connection.invoke('UnsubscribeFromEnvironment', environmentId: string): Promise<void>
```

---

### 2.2 Server → Client Methods

#### `ReceiveMonitoringSnapshot`

Pushed to all connections in the `env-{environmentId}` group each time the backend completes a polling cycle.

```typescript
connection.on('ReceiveMonitoringSnapshot', (snapshot: MonitoringSnapshot) => void)
```

**Payload** (matches `MonitoringSnapshot` TypeScript type in `data-model.md`):
```json
{
  "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2026-04-25T13:01:00Z",
  "server": {
    "version": "2.10.18",
    "connections": 43,
    "totalConnections": 1501,
    "maxConnections": 65536,
    "inMsgsPerSec": 130.1,
    "outMsgsPerSec": 122.5,
    "inBytesPerSec": 67584.0,
    "outBytesPerSec": 33792.0,
    "uptimeSeconds": 86430,
    "memoryBytes": 12845056
  },
  "jetStream": {
    "streamCount": 5,
    "consumerCount": 12,
    "totalMessages": 50130,
    "totalBytes": 2099200
  },
  "status": "Ok"
}
```

---

## 3. Frontend Hook Contract

### `useMonitoringHub(environmentId: string | null)`

```typescript
interface UseMonitoringHubResult {
  snapshots: MonitoringSnapshot[];        // ring buffer (last 120)
  latestSnapshot: MonitoringSnapshot | null;
  connectionStatus: MonitoringConnectionStatus;  // 'connecting' | 'connected' | 'reconnecting' | 'disconnected'
  error: string | null;
}

function useMonitoringHub(environmentId: string | null): UseMonitoringHubResult
```

**Lifecycle**:
1. On mount (with non-null `environmentId`): fetches history from REST endpoint, builds initial `snapshots` array.
2. Starts SignalR connection, invokes `SubscribeToEnvironment`.
3. On each `ReceiveMonitoringSnapshot`: prepends to `snapshots`, trimming to max 120.
4. On unmount: invokes `UnsubscribeFromEnvironment`, stops connection.

---

## 4. Configuration Contract

### `appsettings.json` extension

```json
{
  "Monitoring": {
    "DefaultPollingIntervalSeconds": 30,
    "MaxSnapshotsPerEnvironment": 120,
    "HttpTimeoutSeconds": 10
  }
}
```

| Key | Type | Default | Constraints |
|-----|------|---------|-------------|
| `DefaultPollingIntervalSeconds` | int | 30 | 5–300 |
| `MaxSnapshotsPerEnvironment` | int | 120 | 10–1000 |
| `HttpTimeoutSeconds` | int | 10 | 1–30 |
