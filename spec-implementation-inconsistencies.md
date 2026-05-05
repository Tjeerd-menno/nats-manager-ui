# Spec / Implementation Inconsistencies

This report is maintained during exploratory testing against the specification documents.

## Findings

### 1. Live message viewer did not become active until the first message (fixed)

- **Spec source**: `specs\002-core-nats-subjects-messaging\spec.md` FR-012 requires a real-time message viewer, and SC-003 requires a published message to appear in the viewer within 2 seconds.
- **Observed behavior**: Subscribing opened `/api/environments/{id}/core-nats/stream`, but the browser stayed in the subscribing state because the SSE response did not flush any bytes until a NATS message arrived.
- **Implementation evidence**: `src\NatsManager.Web\Endpoints\CoreNatsEndpoints.cs` streamed only inside the adapter `await foreach`, so an idle subject had no initial event-stream frame.
- **Status**: Fixed. The stream endpoint now flushes an initial SSE comment frame before waiting for messages, and the live viewer reaches `CONNECTED` and receives a published message.

### 2. Alerts, events, and notifications are specified but no dedicated UI is implemented

- **Spec source**: `nats-management-functional-spec.md` section 9.11 requires operational events, recent warnings/failures/significant administrative outcomes, notification support, severities/priorities, and active/resolved/historical status.
- **Observed behavior**: Direct exploratory navigation to `/alerts` and `/events` produced blank unmatched routes, and the frontend router/sidebar exposes no Alerts or Events page.
- **Implementation evidence**: `src\NatsManager.Frontend\src\App.tsx` has routes for dashboard, environments, JetStream, KV, Object Store, services, Core NATS, monitoring, cluster observability, audit, and users, but not alerts/events. Code search found no dedicated alerts/events feature module or endpoint.
- **Status**: Open inconsistency. The audit log covers administrative history, but it is not a substitute for the specified operational alerts/events/notification experience.

### 3. Audit history filtering is only partially exposed in the UI

- **Spec source**: `nats-management-functional-spec.md` section 9.13 requires authorized users to inspect audit history and supports searching and filtering audit history.
- **Observed behavior**: The Audit Log page provides only Action Type and Resource Type selects.
- **Implementation evidence**: The backend query and endpoint support actor, environment, from/to date, and source filters, but `src\NatsManager.Frontend\src\features\audit\AuditPage.tsx` only passes `actionType` and `resourceType`.
- **Status**: Open inconsistency. Backend capability exists, but the UI does not expose search/date/user/environment/source filtering.

### 4. Regular JetStream streams appeared as KV buckets (fixed)

- **Spec source**: `nats-management-functional-spec.md` sections 9.4 and 9.5 distinguish JetStream streams/consumers from Key-Value buckets/keys.
- **Observed behavior**: The KV bucket list showed the regular JetStream stream `my-test-stream` as a KV bucket. Opening it produced 404/500 API failures for the bucket detail and keys endpoints.
- **Implementation evidence**: `KvStoreAdapter.ListBucketsAsync` accepted every status returned by the NATS KV status API except `OBJ_` entries, so regular stream statuses could leak into the KV bucket list.
- **Status**: Fixed. KV bucket listing now only includes statuses bound to `$KV.` subjects and excludes regular JetStream/Object Store streams; browser verification shows no bogus `my-test-stream` KV bucket.
