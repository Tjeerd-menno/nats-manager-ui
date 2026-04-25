# Quickstart: Live Environment Monitoring

**Branch**: `copilot/add-live-monitoring-feature`  
**Date**: 2026-04-25

---

## Prerequisites

- Working NATS Admin UI application (see `specs/001-nats-admin-app/quickstart.md`)
- NATS server with monitoring HTTP API enabled (default port: 8222)
- .NET 10 SDK, Node.js 20+

---

## 1. Enable NATS Monitoring in Your NATS Server

Add to your `nats-server.conf`:

```
http_port: 8222
```

Or pass the flag at startup:

```bash
nats-server --http_port 8222
```

Verify it works:

```bash
curl http://localhost:8222/varz
curl http://localhost:8222/jsz
```

---

## 2. Apply the Database Migration

```bash
cd src/NatsManager.Infrastructure
dotnet ef database update --project NatsManager.Infrastructure.csproj --startup-project ../NatsManager.Web/NatsManager.Web.csproj
```

This applies the `AddEnvironmentMonitoring` migration, which adds `MonitoringUrl` and `MonitoringPollingIntervalSeconds` columns to the `Environments` table.

---

## 3. Configure Monitoring in `appsettings.Development.json`

```json
{
  "Monitoring": {
    "DefaultPollingIntervalSeconds": 15,
    "MaxSnapshotsPerEnvironment": 120,
    "HttpTimeoutSeconds": 10
  }
}
```

---

## 4. Set Monitoring URL on an Environment

Via the UI: navigate to **Environments → [your environment] → Edit**, then fill in the **Monitoring URL** field (e.g., `http://localhost:8222`).

Or via the REST API:

```bash
curl -X PUT http://localhost:5000/api/environments/{envId} \
  -H "Content-Type: application/json" \
  -d '{
    "name": "local",
    "serverUrl": "nats://localhost:4222",
    "monitoringUrl": "http://localhost:8222",
    "monitoringPollingIntervalSeconds": 15
  }'
```

---

## 5. Install Frontend Dependency

```bash
cd src/NatsManager.Frontend
npm install @microsoft/signalr
```

---

## 6. Start the Application

```bash
aspire run
# or
cd src/NatsManager.Web
dotnet run
```

---

## 7. View Live Monitoring

1. Log in to the application.
2. Select an environment that has a monitoring URL configured.
3. Navigate to **Monitoring** in the left sidebar.
4. The Monitoring page connects to the SignalR hub and begins displaying live graphs:
   - **Server Metrics** — connections, message rate (in/out per second), byte rate
   - **JetStream** — stream count, consumer count, total messages trend
5. A green **Connected** badge in the top-right of the Monitoring page confirms the real-time connection.

---

## 8. Run Tests

```bash
# Backend tests (includes new Monitoring module tests)
dotnet test

# Frontend tests
cd src/NatsManager.Frontend
npm test
```
