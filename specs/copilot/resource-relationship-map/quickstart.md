# Quickstart: Resource Relationship Map

**Branch**: `copilot/resource-relationship-map`  
**Date**: 2026-04-28

---

## Prerequisites

- Working NATS Manager UI application.
- Existing resource modules for Core NATS, JetStream, KV, Object Store, Services, Monitoring, and in-app alerts/events concepts.
- .NET 10 SDK and Node.js 22 LTS.
- During implementation only: add and pin `@xyflow/react` in the frontend package for React Flow dependency maps.

> Planning note: this SpecKit planning task does **not** add `@xyflow/react` or edit `package.json`.

---

## 1. Prepare Safe Relationship Data

Use resources already visible in the application:

- streams and consumers;
- subjects and subscriptions where safely available;
- services and endpoints;
- KV buckets/keys and Object Store buckets/objects;
- servers from monitoring/cluster observations;
- active alerts/events with affected resource identifiers.

Payload inspection, live tapping, replay, account JWT inspection, and operator JWT inspection are not required for relationship mapping.

---

## 2. Open a Map from a Resource

1. Log in to NATS Manager UI.
2. Select an environment.
3. Open a supported resource detail page, such as:
   - JetStream stream;
   - consumer;
   - subject;
   - service endpoint;
   - KV bucket/key;
   - Object Store bucket/object;
   - server;
   - alert/event.
4. Select **View Relationship Map**.

Expected result: the map opens with the selected resource as the focal node and shows a bounded one-hop neighborhood by default.

---

## 3. Traverse Dependencies

1. Select a connected node to view evidence, confidence, freshness, and health state.
2. Choose **Recenter map** to make that node the focal resource.
3. Choose **Open details** to navigate to the existing resource detail page.

Expected result: environment context remains visible and unchanged throughout traversal.

---

## 4. Filter Large Maps

Use map filters to reduce visual noise:

- resource type;
- relationship type;
- health state;
- observed vs inferred;
- minimum confidence;
- distance/depth from focal resource;
- stale relationship inclusion.

Expected result: empty-filter states are explicit, hidden branch counts are visible, and direct unhealthy neighbors remain easy to identify.

---

## 5. Validate Alert/Event Links

1. Open a map from an active alert or event.
2. Confirm the affected resource node is highlighted.
3. Verify the alert/event edge includes safe evidence and freshness.

Expected result: in-app alerts/events help identify impact without external notification integrations.

---

## 6. Run Verification

```bash
dotnet test

cd src/NatsManager.Frontend
npm test
npm run build
```

Verification should include relationship projection unit tests, API contract tests for filters and cross-environment rejection, and frontend tests for recentering, filtering, evidence panels, and empty states.

