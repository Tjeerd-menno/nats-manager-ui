# Resource Relationship Map

Developer notes for the frontend relationship-map feature.

## Entry points

Use `OpenRelationshipMapButton` from resource detail surfaces instead of constructing
relationship-map URLs manually:

```tsx
<OpenRelationshipMapButton
  environmentId={environmentId}
  resourceType="Stream"
  resourceId={streamName}
/>
```

The button routes to:

```text
/environments/:envId/relationships?resourceType=:type&resourceId=:id
```

`ResourceRelationshipMapPage` owns filter query parameters (`depth`,
`resourceTypes`, `relationshipTypes`, `healthStates`, `minimumConfidence`,
`includeInferred`, `includeStale`, `maxNodes`, and `maxEdges`) so shared links are
deterministic.

## Supported resource types

The shared frontend type union includes the full long-term relationship vocabulary,
but the currently shipped graph sources emit nodes for streams, consumers, KV,
object store, services, subjects, servers, JetStream accounts, external nodes, and
other safe infrastructure resources. `Alert` and `Event` remain reserved for future
relationship sources and should not be assumed to appear in the current API payloads.

## Safety expectations

Relationship components render only safe metadata provided by the API. Do not add
payload content, credentials, account JWTs, operator JWTs, or raw request/response
bodies to nodes, edges, evidence, URL parameters, or logs.

## Main files

- `ResourceRelationshipMapPage.tsx` — URL parsing, loading/empty/stale states, filters,
  graph, and evidence panel composition.
- `RelationshipFlow.tsx` — React Flow rendering, node selection, and recentering.
- `RelationshipFilters.tsx` — filter controls and shareable query state.
- `RelationshipEvidencePanel.tsx` — selected node/edge evidence details.
- `hooks/useResourceRelationshipMap.ts` — TanStack Query map fetch and navigation helpers.
- `components/OpenRelationshipMapButton.tsx` — reusable entry-point button.
