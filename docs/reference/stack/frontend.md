# Frontend Stack

| Package | Role |
|---|---|
| React 18 + TypeScript | UI framework |
| Vite | Build tool + dev server (proxies `/api` to backend) |
| Tailwind CSS | Utility styling |
| shadcn/ui (Radix UI) | Accessible component library |
| TanStack Query v5 | Server state, polling, cache (5-min stale, 10-min GC) |
| React Router v6 | Client-side routing |
| Recharts | Charts (cost trend, security trends) |
| Framer Motion | Animations (CountUp, FadeIn, Stagger) |
| Lucide React | Icon set |
| axios | HTTP client; Bearer token interceptor |
| Fira Code | Monospace font for stat values/labels |
| Cormorant SC / EB Garamond | Display and body serif type |

## Auth flow (frontend)

Auth state is managed by `AuthProvider` / `useAuth` and persisted in `localStorage`. `ProtectedRoute` guards all dashboard routes — unauthenticated users are redirected to `/login`.

A dev-only helper (`src/lib/devAuth.ts` + `scripts/fetch-dev-token.ps1`) fetches a JWT from the `GET /dev/token` endpoint for local development.
