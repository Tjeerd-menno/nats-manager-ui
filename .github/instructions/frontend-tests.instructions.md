---
description: "Use when writing or modifying frontend Vitest tests or React Testing Library tests. Covers test setup, mocking patterns, renderWithProviders, and component testing conventions."
applyTo: "src/NatsManager.Frontend/src/**/*.test.tsx,src/NatsManager.Frontend/src/**/*.test.ts"
---
# Frontend Test Instructions

## Framework Stack

- **Vitest** — globals enabled (`describe`, `it`, `expect`, `vi` available without import)
- **React Testing Library** — `screen`, `render`, `userEvent`, `waitFor`
- **jsdom** environment
- Test files: colocated as `{Component}.test.tsx` or `hooks/{hook}.test.ts`

## Test Utilities

Use `renderWithProviders` from `src/test-utils.tsx` for component tests. It wraps with `QueryClientProvider`, `MantineProvider`, and `MemoryRouter`:

```typescript
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import MyComponent from './MyComponent';

it('renders heading', () => {
  renderWithProviders(<MyComponent />);
  expect(screen.getByText('My Title')).toBeInTheDocument();
});
```

## Mocking Patterns

### Mock hooks at the module level

```typescript
vi.mock('./hooks/useMyFeature', () => ({
  useMyQuery: vi.fn(),
  useMyMutation: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}));

import { useMyQuery } from './hooks/useMyFeature';
const mockUseMyQuery = vi.mocked(useMyQuery);

beforeEach(() => vi.clearAllMocks());

it('shows data', () => {
  mockUseMyQuery.mockReturnValue({
    data: { name: 'test' },
    isLoading: false,
  } as unknown as ReturnType<typeof useMyQuery>);

  renderWithProviders(<MyPage />);
  expect(screen.getByText('test')).toBeInTheDocument();
});
```

### Hook-level tests (for mutation side effects)

```typescript
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../../api/client', () => ({
  apiClient: { post: vi.fn(() => Promise.resolve({ data: {} })) },
}));

it('invalidates query on success', async () => {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  const spy = vi.spyOn(queryClient, 'invalidateQueries');
  // ... renderHook, mutate, waitFor, assert spy called
});
```

## Conventions

- Use `screen.getByRole`, `screen.getByText`, `screen.getByLabelText` — prefer accessible queries
- Use `userEvent.setup()` over `fireEvent` for user interactions
- Assert loading states via Mantine CSS class: `container.querySelector('.mantine-Loader-root')`
- Test files live next to their source — never in a separate `__tests__/` folder

## Running Tests

```bash
cd src/NatsManager.Frontend
npm test                          # All tests
npx vitest run src/features/...   # Specific file
npm test -- --coverage            # With coverage
```
