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

### 5. A single configured environment was not selected by default (fixed)

- **Spec source**: `nats-management-functional-spec.md` section 9.1 describes the dashboard as an overview of managed NATS environments, and feature pages throughout sections 9.3-9.10 are environment-scoped operational views.
- **Observed behavior**: After a fresh login with exactly one configured environment, the dashboard and environment-scoped feature pages stayed in their "Select an environment" state until `sessionStorage['nats-admin:selectedEnvironmentId']` was set manually or the user explicitly chose the only option.
- **Implementation evidence**: `EnvironmentProvider` initialized selection only from session storage, and `EnvironmentSelector` did not auto-select the single returned environment.
- **Status**: Fixed. `EnvironmentSelector` now auto-selects the only available environment when no selection exists, with a focused frontend regression test and browser verification after clearing session storage.

### 6. KV bucket summary stayed stale after key writes (fixed)

- **Spec source**: `specs\001-nats-admin-app\spec.md` US4 and related KV tasks require users to inspect KV buckets and keys, including metadata and current key state.
- **Observed behavior**: Creating a key in `qa-bucket` updated the key table immediately, but the bucket summary cards still showed `Keys 0` and stale size data until a full reload/refetch.
- **Implementation evidence**: `usePutKvKey` invalidated key and history queries, but not the bucket-detail or bucket-list queries that drive the summary counts.
- **Status**: Fixed. KV key writes and deletes now invalidate bucket detail/list queries; a hook regression test covers the invalidation and browser verification showed the summary updating to `Keys 2`.

### 7. Relationship maps returned edges whose endpoint nodes were missing (fixed)

- **Spec source**: `specs\copilot\resource-relationship-map\spec.md` requires a topology graph of connected resources with bounded nodes and edges.
- **Observed behavior**: Opening the relationship map for a KV bucket with keys returned `1 node(s) · 3 edge(s)`, and the API payload contained edges targeting KV key/backing stream node IDs that were not present in the `nodes` array.
- **Implementation evidence**: `RelationshipProjectionService` filtered edges before node resolution, but did not remove edges whose endpoint nodes could not be resolved. `KeyValueRelationshipSource` and `ObjectStoreRelationshipSource` also emitted key/object edges without resolving those target node types.
- **Status**: Fixed. The projection service now omits unresolved dangling edges, KV/Object Store relationship sources resolve key/object nodes, and browser verification shows KV/Object Store maps with concrete child nodes and no dangling visible edges.

### 8. Global search only searches bookmarks, not all resources

- **Spec source**: `nats-management-functional-spec.md` section 9.10 and the relationship-map planning notes describe cross-module Search as an application-level resource discovery surface.
- **Observed behavior**: After creating `qa-bucket`, typing `qa-bucket` into the global "Search resources..." box returned `No results found`.
- **Implementation evidence**: `/api/search?q=qa-bucket` returned `[]`; `SearchQueryHandler` searches only the current user's bookmarks via `IBookmarkRepository`, not live environments, streams, KV buckets/keys, object buckets/objects, services, or audit-visible resources.
- **Status**: Open inconsistency. Bookmark search works, but the global search label and spec imply cross-resource discovery.

### 9. Governance/settings routes from specs are not implemented

- **Spec source**: `specs\copilot\governance-hardening\spec.md` and root authorization/governance sections describe governance controls, scoped roles, and administrative settings.
- **Observed behavior**: Direct navigation to `/settings/users`, `/settings/roles`, `/settings/governance`, and `/access-control` produced unmatched/blank routes during exploratory testing.
- **Implementation evidence**: `src\NatsManager.Frontend\src\App.tsx` exposes `/admin/users` for users but no settings/governance/access-control routes or dedicated roles management page.
- **Status**: Open inconsistency. Some backend role/scoped-role enforcement exists, but the specified administrative governance UI is incomplete.

### 10. Standalone bookmarks/search pages from specs are not routed

- **Spec source**: `nats-management-functional-spec.md` section 9.10 includes bookmarks and saved views alongside search/discovery.
- **Observed behavior**: Direct navigation to `/bookmarks` and `/search` produced unmatched/blank routes during exploratory testing.
- **Implementation evidence**: `GlobalSearch` and `BookmarkList` components exist under `src\NatsManager.Frontend\src\features\search\SearchPage.tsx`, but `src\NatsManager.Frontend\src\App.tsx` does not register `/search` or `/bookmarks`.
- **Status**: Open inconsistency. Search/bookmark components are embedded/available as building blocks, but the standalone routed surfaces are missing.
