# Data Model: Live Environment Monitoring

**Branch**: `copilot/add-live-monitoring-feature`  
**Date**: 2026-04-25

---

## 1. Domain Entity Changes

### `Environment` (extended)

Existing entity in `NatsManager.Domain.Modules.Environments`. Two new nullable fields are added:

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `MonitoringUrl` | `string` | Yes | HTTP URL of the NATS monitoring API (e.g., `http://host:8222`). If null, monitoring is disabled for this environment. |
| `MonitoringPollingIntervalSeconds` | `int` | Yes | Per-environment override for polling interval. If null, the global default (`Monitoring:DefaultPollingIntervalSeconds`) is used. |

**State rules**:
- If `MonitoringUrl` is null, the environment is excluded from polling — no monitoring data is collected.
- If `MonitoringUrl` is set but unreachable, polling continues; errors are logged and the frontend receives `MonitoringUnavailable` status.
- `MonitoringPollingIntervalSeconds` must be between 5 and 300 (inclusive) when provided.

---

## 2. New Value Object / Read Model

### `MonitoringSnapshot`

A point-in-time metric record captured from one polling cycle. Lives in `Application.Modules.Monitoring.Models`.

```
MonitoringSnapshot
├── EnvironmentId      : Guid
├── Timestamp          : DateTimeOffset (UTC)
├── Server             : ServerMetrics
│   ├── Version        : string
│   ├── Connections    : int
│   ├── TotalConnections : long
│   ├── MaxConnections : int
│   ├── InMsgsTotal    : long     (cumulative counter from /varz)
│   ├── OutMsgsTotal   : long
│   ├── InBytesTotal   : long
│   ├── OutBytesTotal  : long
│   ├── InMsgsPerSec   : double   (derived: delta / intervalSeconds)
│   ├── OutMsgsPerSec  : double
│   ├── InBytesPerSec  : double
│   ├── OutBytesPerSec : double
│   ├── UptimeSeconds  : long
│   └── MemoryBytes    : long
├── JetStream          : JetStreamMetrics?  (null if JetStream disabled)
│   ├── StreamCount    : int
│   ├── ConsumerCount  : int
│   ├── TotalMessages  : long
│   └── TotalBytes     : long
└── Status             : MonitoringStatus (enum: Ok | Degraded | Unavailable)
```

**Derivation**: `InMsgsPerSec = (current.InMsgsTotal - previous.InMsgsTotal) / intervalSeconds`. If there is no previous snapshot, rate fields are `0`.

---

## 3. In-Memory Store

### `MonitoringMetricsStore`

Lives in `Infrastructure.Monitoring`. Not persisted to SQLite.

```
MonitoringMetricsStore
└── _store : ConcurrentDictionary<Guid, EnvironmentMetricsBuffer>

EnvironmentMetricsBuffer
├── Snapshots     : Queue<MonitoringSnapshot>  (capped at MaxSnapshotsPerEnvironment)
├── MaxCapacity   : int
└── Lock          : object  (for thread-safe enqueue/dequeue)
```

**Interface** (in `Application.Modules.Monitoring.Ports`):
```csharp
public interface IMonitoringMetricsStore
{
    void AddSnapshot(MonitoringSnapshot snapshot);
    IReadOnlyList<MonitoringSnapshot> GetHistory(Guid environmentId);
    MonitoringSnapshot? GetLatest(Guid environmentId);
}
```

---

## 4. Configuration Options

### `MonitoringOptions`

Bound from `appsettings.json` section `"Monitoring"`.

```
MonitoringOptions
├── DefaultPollingIntervalSeconds  : int  (default: 30, range: 5–300)
├── MaxSnapshotsPerEnvironment     : int  (default: 120)
└── HttpTimeoutSeconds             : int  (default: 10)
```

---

## 5. NATS HTTP API Response Models

Internal deserialization models used only by `NatsMonitoringHttpAdapter`. Not exposed to the rest of the application.

### `NatsVarzResponse` (maps `/varz`)

| JSON field | C# property | Type |
|-----------|-------------|------|
| `server_id` | `ServerId` | `string` |
| `version` | `Version` | `string` |
| `connections` | `Connections` | `int` |
| `total_connections` | `TotalConnections` | `long` |
| `max_connections` | `MaxConnections` | `int` |
| `in_msgs` | `InMsgs` | `long` |
| `out_msgs` | `OutMsgs` | `long` |
| `in_bytes` | `InBytes` | `long` |
| `out_bytes` | `OutBytes` | `long` |
| `uptime` | `Uptime` | `string` (e.g., "1d2h3m4s") |
| `mem` | `Mem` | `long` |

### `NatsJszResponse` (maps `/jsz`)

| JSON field | C# property | Type |
|-----------|-------------|------|
| `streams` | `Streams` | `int` |
| `consumers` | `Consumers` | `int` |
| `messages` | `Messages` | `long` |
| `bytes` | `Bytes` | `long` |

---

## 6. Database Schema Change

**Migration**: `AddEnvironmentMonitoring`

```sql
ALTER TABLE Environments ADD COLUMN MonitoringUrl TEXT NULL;
ALTER TABLE Environments ADD COLUMN MonitoringPollingIntervalSeconds INTEGER NULL;
```

EF Core migration class in `NatsManager.Infrastructure.Migrations`.

---

## 7. Validation Rules

| Field | Rule |
|-------|------|
| `MonitoringUrl` | If provided: must start with `http://` or `https://`; must be a valid URI; max 500 chars |
| `MonitoringPollingIntervalSeconds` | If provided: integer between 5 and 300 inclusive |

Applied via FluentValidation in the `UpdateEnvironmentCommand` validator (existing validator extended).

---

## 8. Frontend Types

```typescript
// src/features/monitoring/types.ts

export interface MonitoringSnapshot {
  environmentId: string;
  timestamp: string;           // ISO 8601
  server: ServerMetrics;
  jetStream: JetStreamMetrics | null;
  status: 'Ok' | 'Degraded' | 'Unavailable';
}

export interface ServerMetrics {
  version: string;
  connections: number;
  totalConnections: number;
  maxConnections: number;
  inMsgsPerSec: number;
  outMsgsPerSec: number;
  inBytesPerSec: number;
  outBytesPerSec: number;
  uptimeSeconds: number;
  memoryBytes: number;
}

export interface JetStreamMetrics {
  streamCount: number;
  consumerCount: number;
  totalMessages: number;
  totalBytes: number;
}

export type MonitoringConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
```
