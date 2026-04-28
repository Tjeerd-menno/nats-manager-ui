# Implementation Plan: Cluster Observability

**Branch**: `copilot/cluster-observability` | **Date**: 2026-04-28 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/copilot/cluster-observability/spec.md`

## Summary

Add an environment-scoped Cluster Observability feature that builds on the existing Monitoring module and live monitoring concepts to summarize NATS server health, compare server-level metrics, and visualize safe route/gateway/leafnode topology. The backend expands monitoring read models with cluster observations collected from NATS HTTP monitoring endpoints (`/varz`, `/jsz`, `/healthz`, `/routez`, `/gatewayz`, `/leafz`, and compatible discovery metadata when available). The frontend adds a Monitoring-aligned cluster page with sortable server observations and a React Flow topology canvas using `@xyflow/react` for route, gateway, and leafnode relationships.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, existing Monitoring module/poller/store patterns, NATS HTTP monitoring API, TanStack Query, Mantine 9, Recharts 3.8.1, `@xyflow/react` (React Flow; new frontend dependency to add during implementation only)  
**Storage**: Ephemeral in-memory cluster observation cache/ring buffer; no SQLite persistence for monitoring payloads or topology observations  
**Testing**: xUnit + NSubstitute for application/infrastructure adapters, ASP.NET Core endpoint contract tests, Vitest + React Testing Library for frontend pages/components, React Flow canvas tests through accessible controls and deterministic layout fixtures  
**Target Platform**: Desktop browsers; same Linux OCI container and Aspire development flow as the main app  
**Project Type**: Feature addition to existing web application (SPA + Minimal API)  
**Performance Goals**: Cluster overview initial data ≤ 1s after API response; topology layout for 250 observed servers/relationships ≤ 2s; local search/filter interactions ≤ 300ms; server-backed refresh queries ≤ 1s; monitoring endpoint timeouts bounded at 10s  
**Constraints**: Read-only observability; environment context required on every request; safe metadata only; no account/operator JWT or payload persistence; topology data may be partial or stale; do not add `@xyflow/react` to `package.json` during this planning task  
**Scale/Scope**: NATS 2.x, targeting NATS 2.10+; at least 250 observed servers/relationships; route, gateway, leafnode, and cluster peer signals where monitoring endpoints expose them

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality (NON-NEGOTIABLE) — ✅ PASS

- Feature extends the existing `Monitoring` bounded context instead of creating a duplicate observability subsystem.
- Backend contracts separate endpoint fetching, snapshot normalization, health derivation, and API presentation.
- TypeScript models explicitly represent live/stale/derived/unavailable metric states; no `any` types are needed.
- NATS terminology is kept exact: servers, routes, gateways, leafnodes, clusters, peers, streams, consumers.
- New dependency is isolated to frontend visualization: `@xyflow/react` is justified for graph canvas interactions and must be version-pinned when implementation adds it.

### II. Testing Standards (NON-NEGOTIABLE) — ✅ PASS

- Unit tests cover topology normalization from `/routez`, `/gatewayz`, and `/leafz`, warning-state derivation, freshness calculation, and environment isolation.
- Contract tests validate cluster overview/topology API responses and unavailable/partial/stale states.
- Frontend tests cover overview cards, sortable/filterable server list, topology empty/partial states, and keyboard-accessible node selection.
- NATS monitoring HTTP calls are mocked at adapter boundaries; no test depends on a live external monitoring endpoint.

### III. User Experience Consistency — ✅ PASS

- Cluster Observability lives under Monitoring and reuses existing environment context, loading/error/stale states, freshness indicators, and summary-card patterns.
- Detail layout follows identity → status → metrics → relationships → actions, with no state-changing actions added.
- Topology visualization includes a tabular/list fallback and accessible node metadata panel.
- Unavailable monitoring data gives actionable guidance to verify monitoring configuration.

### IV. Performance Requirements — ✅ PASS

- Cluster snapshots remain in bounded memory caches; no unbounded polling history or SQLite writes.
- Server lists use existing search/filter/list patterns and can adopt virtualization for large lists.
- React Flow renders a bounded topology by default and filters/simplifies large graphs before drawing.
- All outbound monitoring calls use bounded 10s timeouts and degrade to partial/unavailable observations.

## Overlap / Main-State Note

`origin/main` currently does not contain the new `specs/copilot/cluster-observability` or `specs/copilot/resource-relationship-map` spec folders, does not contain `@xyflow/react` or `reactflow` in the frontend package, and only has generic relationship/topology references in existing root functional/spec guidance. This plan deliberately builds on the existing Monitoring module, live monitoring concepts/endpoints, shared resource navigation, and environment context rather than duplicating those modules.

## Project Structure

### Documentation (this feature)

```text
specs/copilot/cluster-observability/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── contracts/
    └── api-contracts.md # Phase 1 output
```

### Source Code (repository root)

```text
src/
├── NatsManager.Application/
│   └── Modules/
│       └── Monitoring/
│           ├── Models/
│           │   ├── ClusterObservation.cs
│           │   ├── ServerObservation.cs
│           │   └── TopologyRelationship.cs
│           ├── Ports/
│           │   ├── IClusterMonitoringAdapter.cs
│           │   └── IClusterObservationStore.cs
│           └── Queries/
│               ├── GetClusterOverviewQuery.cs
│               └── GetClusterTopologyQuery.cs
│
├── NatsManager.Infrastructure/
│   ├── Monitoring/
│   │   └── ClusterObservationStore.cs
│   └── Nats/
│       └── NatsClusterMonitoringHttpAdapter.cs
│
├── NatsManager.Web/
│   └── Endpoints/
│       └── ClusterObservabilityEndpoints.cs
│
└── NatsManager.Frontend/
    └── src/
        └── features/
            └── monitoring/
                └── cluster-observability/
                    ├── ClusterObservabilityPage.tsx
                    ├── ClusterServerList.tsx
                    ├── ClusterTopologyFlow.tsx      # React Flow via @xyflow/react
                    ├── ClusterTopologyLegend.tsx
                    ├── hooks/
                    │   └── useClusterObservability.ts
                    └── types.ts

tests/
├── NatsManager.Application.Tests/
│   └── Modules/Monitoring/ClusterObservability/
├── NatsManager.Infrastructure.Tests/
│   └── Monitoring/ClusterObservability/
└── NatsManager.Web.Tests/
    └── Monitoring/ClusterObservability/
```

**Structure Decision**: Implement as an extension of the existing `Monitoring` bounded context. Cluster observability is read-only operational telemetry and should reuse monitoring configuration, freshness semantics, endpoint authorization, and UI placement. No new .NET projects or persisted aggregates are required.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New frontend dependency (`@xyflow/react`) | Route/gateway/leaf topology requires pan/zoom, node/edge rendering, selection, and layout-ready graph primitives | Tables alone cannot satisfy topology visualization; hand-rolled SVG would duplicate accessibility and interaction work already solved by React Flow |
