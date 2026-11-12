# Hooks & State

| Hook | File | Purpose |
|---|---|---|
| `useAuth` | `hooks/useAuth.tsx` | Auth state, login/logout, token storage in localStorage |
| `useApi` | `hooks/useApi.ts` | All API calls via TanStack Query; axios with Bearer interceptor |
| `useTheme` | `hooks/useTheme.tsx` | Dark/light theme context |
| `useThemeCustomizer` | `hooks/useThemeCustomizer.tsx` | Extended theme customisation |
| `useLayout` | `hooks/useLayout.tsx` | Sidebar open/close state |

## API hook examples

```ts
// Provided by useApi.ts
const { data: agents } = useAgents()
const { data: health } = useHealthStatus()
const { data: violations } = useSecurityViolations()
const { data: costTrend } = useCostTrend()
const { data: traces } = useTraces()
```

TanStack Query is configured globally with a 5-minute stale time and a 10-minute cache time.
