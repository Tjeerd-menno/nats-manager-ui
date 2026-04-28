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

The frontend type union currently supports:

- `Server`
- `Subject`
- `Stream`
- `Consumer`
- `KvBucket`
- `KvKey`
- `ObjectBucket`
- `Object` / `ObjectStoreObject`
- `Service`
- `Endpoint` / `ServiceEndpoint`
- `Alert`
- `Event`
- `External`
- `JetStreamAccount`
- `Client`

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
