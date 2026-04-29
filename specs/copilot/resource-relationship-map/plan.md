# Implementation Plan: Resource Relationship Map

**Branch**: `copilot/resource-relationship-map` | **Date**: 2026-04-28 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/copilot/resource-relationship-map/spec.md`

## Summary

Add an environment-scoped Resource Relationship Map that lets operators open a focal-resource dependency graph from streams, consumers, subjects, services, KV buckets/keys, Object Store buckets/objects, servers, alerts, and events. The feature builds on existing resource modules, search/navigation concepts, Monitoring observations, and alerts/events guidance to produce a bounded, safe metadata graph. The frontend renders focal-resource dependency maps with React Flow using `@xyflow/react`, while backend read models aggregate observed and inferred relationships without persisting payload content.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, existing Core NATS/JetStream/KV/Object Store/Services/Monitoring modules, Search/resource navigation concepts, Alerts/Events concepts, TanStack Query, Mantine 9, `@xyflow/react` (React Flow; new frontend dependency to add during implementation only)  
**Storage**: Ephemeral relationship projections derived from existing resource read models and safe monitoring metadata; optional in-memory cache for graph responses; no payload or JWT persistence  
**Testing**: xUnit + NSubstitute for relationship projection/query services, Web contract tests for graph filters/errors, Vitest + React Testing Library for map controls and detail-panel interactions  
**Target Platform**: Desktop browsers; same Linux OCI container and Aspire development flow as the main app  
**Project Type**: Feature addition to existing web application (SPA + Minimal API)  
**Performance Goals**: Initial bounded neighborhood for up to 500 available relationships renders in ≤ 2s; local map filters respond ≤ 300ms for rendered graph; API p95 ≤ 1s for bounded graph queries  
**Constraints**: Environment-scoped; focal-resource required; read-only; graph defaults to bounded depth; safe metadata only; no payload reveal, live tapping, replay, account JWT, or operator JWT inspection; do not add `@xyflow/react` to `package.json` during this planning task  
**Scale/Scope**: Supported NATS 2.x targeting NATS 2.10+ metadata behavior; direct and bounded multi-hop relationships across resource modules, alerts, and events

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality (NON-NEGOTIABLE) — ✅ PASS

- Relationship projection is a read-model/query concern that composes existing modules rather than copying their resource logic.
- Graph DTOs have explicit node, edge, evidence, confidence, and freshness types.
- NATS terminology remains exact for streams, consumers, subjects, services, buckets, objects, servers, alerts, and events.
- `@xyflow/react` is the only planned new frontend dependency and is justified for graph interaction.

### II. Testing Standards (NON-NEGOTIABLE) — ✅ PASS

- Unit tests cover relationship extraction from each supported resource family and confidence/freshness derivation.
- Contract tests cover bounded depth, filters, focal resource validation, unavailable partial data, and cross-environment rejection.
- Frontend tests cover opening from detail pages, recentering, filter application, empty-filter state, alert highlighting, and keyboard-accessible node actions.
- Mocks are limited to existing module query ports and external monitoring adapters.

### III. User Experience Consistency — ✅ PASS

- Entry points are added to existing resource detail pages and alert/event detail surfaces, preserving environment context.
- The map follows existing detail-view structure by showing focal identity, status, relationships, evidence, and navigation actions.
- Loading, empty, partial, stale, and error states are explicit.
- Users can navigate to resource details or recenter on a connected node without losing environment context.

### IV. Performance Requirements — ✅ PASS

- API returns bounded neighborhoods by default and requires explicit expansion/filtering for large maps.
- The frontend filters graph data before React Flow rendering where possible.
- Map state is URL/query driven enough to avoid unnecessary recomputation and support deterministic tests.
- No background graph persistence or unbounded payload inspection is introduced.

## Overlap / Main-State Note

`origin/main` currently does not contain the new `specs/copilot/cluster-observability` or `specs/copilot/resource-relationship-map` spec folders, does not contain `@xyflow/react` or `reactflow` in the frontend package, and only has generic relationship/topology references in existing root functional/spec guidance. This plan builds on existing Monitoring, resource modules, search/navigation, and alerts/events concepts rather than duplicating resource ownership or creating a second search/indexing system.

## Project Structure

### Documentation (this feature)

```text
specs/copilot/resource-relationship-map/
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
│       ├── Relationships/                      # NEW read-model module only
│       │   ├── Models/
│       │   │   ├── ResourceNode.cs
│       │   │   ├── RelationshipEdge.cs
│       │   │   ├── RelationshipEvidence.cs
│       │   │   └── RelationshipMap.cs
│       │   ├── Ports/
│       │   │   └── IRelationshipSource.cs
│       │   └── Queries/
│       │       └── GetResourceRelationshipMapQuery.cs
│       ├── CoreNats/                            # Existing resource query sources
│       ├── JetStream/
│       ├── KeyValue/
│       ├── ObjectStore/
│       ├── Services/
│       ├── Monitoring/
│       └── Alerts/
│
├── NatsManager.Infrastructure/
│   └── Relationships/
│       └── RelationshipProjectionService.cs
│
├── NatsManager.Web/
│   └── Endpoints/
│       └── RelationshipMapEndpoints.cs
│
└── NatsManager.Frontend/
    └── src/
        └── features/
            ├── relationships/
            │   ├── ResourceRelationshipMapPage.tsx
            │   ├── RelationshipFlow.tsx          # React Flow via @xyflow/react
            │   ├── RelationshipFilters.tsx
            │   ├── RelationshipEvidencePanel.tsx
            │   ├── hooks/
            │   │   └── useResourceRelationshipMap.ts
            │   └── types.ts
            ├── jetstream/                        # Add map entry links to existing details
            ├── kv/
            ├── objectstore/
            ├── services/
            ├── corenats/
            ├── monitoring/
            └── alerts/

tests/
├── NatsManager.Application.Tests/
│   └── Modules/Relationships/
├── NatsManager.Infrastructure.Tests/
│   └── Relationships/
└── NatsManager.Web.Tests/
    └── Relationships/
```

**Structure Decision**: Add a relationship read-model module that composes existing module queries and safe monitoring/alert metadata. Ownership of streams, consumers, services, KV, Object Store, servers, alerts, and events remains in their existing modules.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New frontend dependency (`@xyflow/react`) | Focal-resource dependency maps require graph layout primitives, pan/zoom, node/edge selection, and map controls | A table-only implementation would not satisfy the map requirement; hand-built SVG would duplicate established React Flow behavior |
| New read-model module (`Relationships`) | Relationships span multiple existing bounded contexts and need one composition/query boundary | Adding relationship logic into each resource module would duplicate traversal/filter/evidence behavior and make cross-module consistency harder |

