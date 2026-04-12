---
description: "Use when writing or modifying TypeScript/React frontend code. Covers feature-folder structure, hooks, components, Mantine UI, TanStack Query patterns, and TypeScript strict mode."
applyTo: "src/NatsManager.Frontend/**"
---
# Frontend TypeScript/React Instructions

## Feature-Folder Structure

Each domain feature lives under `src/features/{feature}/`:

```
features/{feature}/
├── {Feature}Page.tsx          # Main page component
├── {Feature}Page.test.tsx     # Page tests
├── hooks/
│   └── use{Feature}.ts        # TanStack Query hooks (queries + mutations)
├── components/
│   ├── {Component}.tsx
│   └── {Component}.test.tsx
└── types.ts                   # Feature-specific TypeScript types
```

Shared/reusable components go in `src/shared/`. API setup lives in `src/api/`.

## TanStack Query Hooks

- Queries use `useQuery` with `queryKey` arrays: `['resource-name', id]`
- Mutations use `useMutation` and should invalidate related queries on success via `onSuccess` + `queryClient.invalidateQueries`
- API calls go through the shared `apiClient` (axios instance at `src/api/client.ts`)
- Never call `apiClient` directly from components — always via hooks

```typescript
export function useItems(envId: string | null) {
  return useQuery({
    queryKey: ['items', envId],
    queryFn: async () => {
      const response = await apiClient.get(`/environments/${envId}/items`);
      return response.data as Item[];
    },
    enabled: !!envId,
  });
}

export function useCreateItem(envId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: CreateItemRequest) => {
      await apiClient.post(`/environments/${envId}/items`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['items', envId] });
    },
  });
}
```

## UI Components

- Use **Mantine 7** components: `Card`, `Stack`, `Group`, `SimpleGrid`, `Badge`, `Button`, `TextInput`, `Modal`, `Table`, etc.
- Use `@tabler/icons-react` for icons
- Destructive actions must use `ConfirmActionDialog` from `src/shared/`
- Loading states: use `LoadingState` component or Mantine `Loader`
- Empty states: use `EmptyState` component

## TypeScript

- Strict mode is enabled — all types must be explicit at boundaries
- Use `type` imports for type-only references: `import type { Foo } from './types'`
- No `any` — use `unknown` and narrow
- 2-space indentation (enforced by .editorconfig)

## Naming

- Components: `PascalCase.tsx` (e.g., `StreamDetail.tsx`)
- Hooks: `camelCase.ts` prefixed with `use` (e.g., `useJetStream.ts`)
- Types: `PascalCase` (e.g., `StreamInfo`, `CreateStreamRequest`)
- Test files: `{Component}.test.tsx` colocated with their source
