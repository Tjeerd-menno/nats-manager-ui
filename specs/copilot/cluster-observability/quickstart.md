# Quickstart: Cluster Observability

**Branch**: `copilot/cluster-observability`  
**Date**: 2026-04-28

---

## Prerequisites

- Working NATS Manager UI application.
- Existing live Monitoring feature concepts available for environment monitoring configuration.
- NATS server or cluster with HTTP monitoring enabled (commonly port `8222`).
- .NET 10 SDK and Node.js 22 LTS.
- During implementation only: add and pin `@xyflow/react` in the frontend package for React Flow topology rendering.

> Planning note: this SpecKit planning task does **not** add `@xyflow/react` or edit `package.json`.

---

## 1. Enable NATS Monitoring Endpoints

Configure each NATS server with HTTP monitoring enabled:

```bash
nats-server --http_port 8222
```

For clustered/topology validation, verify the relevant endpoints are available where your deployment exposes them:

```bash
curl http://localhost:8222/healthz
curl http://localhost:8222/varz
curl http://localhost:8222/jsz
curl http://localhost:8222/routez
curl http://localhost:8222/gatewayz
curl http://localhost:8222/leafz
```

Missing `/gatewayz` or `/leafz` data should produce partial/unavailable topology sections rather than failing the whole page.

---

## 2. Configure Monitoring for an Environment

Use the existing environment monitoring configuration to set the monitoring base URL for the selected environment, for example:

```text
Monitoring URL: http://localhost:8222
Polling interval: 15-30 seconds
```

Cluster Observability must inherit this environment context and must not combine observations across environments.

---

## 3. Open Cluster Observability

1. Log in to NATS Manager UI.
2. Select an environment with monitoring configured.
3. Navigate to **Monitoring → Cluster Observability**.
4. Confirm that the overview shows:
   - overall cluster status and freshness;
   - server count and degraded server count;
   - connection pressure and message/byte rates when available;
   - JetStream availability from `/jsz`;
   - last observed timestamp.

---

## 4. Compare Servers

1. Use the server list to sort by connections, slow consumers, memory, version, status, or last observed time.
2. Filter by warning state or version.
3. Expand a server row to inspect recent trend context and metric freshness labels.

Expected result: missing fields are labeled unavailable, stale servers remain visible with last-seen time, and warning signals are visually prioritized.

---

## 5. Inspect Topology

1. Open the **Topology** tab/section.
2. Verify route, gateway, leafnode, and cluster peer relationships from `/routez`, `/gatewayz`, and `/leafz`.
3. Use React Flow controls to pan, zoom, select nodes, and inspect edge metadata.
4. Apply filters for relationship type, warning state, and stale relationships.

Expected result: unsafe account/operator details are omitted, external relationships are labeled, and partial topology states remain actionable.

---

## 6. Run Verification

```bash
dotnet test

cd src/NatsManager.Frontend
npm test
npm run build
```

Verification should include API contract tests for unavailable/partial endpoint responses and frontend tests for overview, server list, topology filtering, and empty states.

