# SeldonVigil Frontend

A modern React + TypeScript + Vite frontend for the SeldonVigil enterprise-grade observability, security, and governance platform.

## 🚀 Quick Start

### Prerequisites
- Node.js 18+ and npm/pnpm/yarn

### Installation

```bash
cd frontend

# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

The frontend will be available at `http://localhost:5173` by default.

## 📁 Project Structure

```
frontend/
├── src/
│   ├── components/        # React components
│   │   ├── charts/       # Chart components (CostTrendChart)
│   │   ├── layout/       # Layout components (DashboardLayout, Sidebar, Header)
│   │   ├── trace/        # Trace visualization components (TraceTree)
│   │   └── ui/           # Reusable UI components (Button, Card, Input, Skeleton)
│   ├── hooks/            # Custom React hooks (useApi)
│   ├── lib/              # Utility functions
│   ├── pages/            # Page components (Traces.tsx)
│   ├── App.tsx           # Main app component with routing
│   ├── main.tsx          # React DOM entry point
│   └── index.css         # Global styles with Tailwind CSS
├── index.html            # HTML entry point
├── vite.config.ts        # Vite configuration
├── tsconfig.json         # TypeScript configuration
├── tailwind.config.js    # Tailwind CSS configuration
├── postcss.config.js     # PostCSS configuration
├── package.json          # Dependencies and scripts
└── .env.example          # Environment variables template
```

## 🛠️ Available Scripts

- `npm run dev` - Start development server with hot module replacement
- `npm run build` - Build for production (optimized and minified)
- `npm run lint` - Run ESLint to check code quality
- `npm run preview` - Preview the production build locally

## 🎨 Technologies

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Fast build tool and dev server
- **React Router** - Client-side routing
- **TanStack Query (React Query)** - Server state management
- **Recharts** - Charts and data visualization
- **Tailwind CSS** - Utility-first CSS framework
- **Lucide Icons** - Modern icon library
- **Axios** - HTTP client

## 📡 API Integration

### API base URL

The API base is a relative path (`/api/v1`), so one build works both behind the nginx
proxy in the container and behind the Vite dev proxy locally. No environment variable
is required.

### API Hooks

All API calls are managed through custom hooks in `src/hooks/useApi.ts`:

```typescript
import { useCostTrend, useTraces, useTraceDetails } from '@/hooks/useApi'

// Use in components
const { data, isLoading, error } = useTraces({ agentId: '123' })
```

Available hooks:
- `useCostTrend()` - Fetch cost trend data
- `useTraces()` - Fetch traces with filtering
- `useTraceDetails()` - Fetch details for a specific trace
- `useSecurityViolations()` - Fetch security violations
- `useHealthStatus()` - Fetch health status

## 📦 Proxying API Requests

During development, API requests are automatically proxied to the backend via Vite's proxy configuration. Requests to `/api/*` are forwarded to `http://localhost:8080/api/*`.

To change the backend URL, update `vite.config.ts`:

```typescript
server: {
  proxy: {
	'/api': {
	  target: 'http://your-backend-url:8080',
	  // ...
	}
  }
}
```

## 🎯 Features

### Dashboard Layout
- Responsive sidebar navigation
- Collapsible menu
- Header with notifications and user menu
- Dark/Light mode support (via Tailwind)

### Pages

#### Trace Explorer (`/traces`)
- Visual decision-tree debugging for multi-agent systems
- Search and filter traces
- View detailed trace information
- Export trace data

### Components

#### Charts
- **CostTrendChart** - Displays cost trends over time with budget reference lines

#### Trace Visualization
- **TraceTree** - Visual representation of trace decision trees

## 🔄 Development Workflow

1. Start the dev server: `npm run dev`
2. Make changes to components
3. Hot module replacement will update the UI automatically
4. Run `npm run lint` to check for code quality issues
5. Build with `npm run build` before deploying

### Local dev authentication helper

When running the API locally without an external authority, the backend exposes a development-only endpoint that issues a signed JWT: `GET /dev/token` (Development environment only).

To fetch a token and save it locally you can use the included PowerShell helper:

From the `frontend` directory run:

```powershell
./scripts/fetch-dev-token.ps1 -Url http://localhost:8080 -Hours 2 -Role admin -Sub test-user
```

The script saves the token to `frontend/.devtoken` and prints a curl example. Use the token as the `Authorization: Bearer <token>` header when calling the API during local development.

## 📚 Adding New Pages

1. Create a new file in `src/pages/`
2. Create the page component
3. Add a route in `src/App.tsx`

Example:

```typescript
// src/pages/NewPage.tsx
export function NewPage() {
  return <div>New Page Content</div>
}

// Update src/App.tsx
<Route path="/new-page" element={<NewPage />} />
```

## 🎨 Adding New UI Components

UI components are located in `src/components/ui/` and use Tailwind CSS with CVA (class-variance-authority) for variants.

Example:

```typescript
// src/components/ui/dialog.tsx
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const dialogVariants = cva(/* ... */)

export function Dialog({ variant, ...props }) {
  return <div className={cn(dialogVariants({ variant }))}>
	{/* ... */}
  </div>
}
```

## 📱 Responsive Design

The frontend is fully responsive and tested on:
- Mobile (320px+)
- Tablet (768px+)
- Desktop (1024px+)
- Large screens (1280px+)

## 🚀 Production Deployment

### Build

```bash
npm run build
```

This creates an optimized production build in the `dist/` directory.

### Deploy

The `dist/` folder can be served by any static hosting service:
- Vercel
- Netlify
- GitHub Pages
- AWS S3 + CloudFront
- Azure Static Web Apps

**Important**: Configure your server to rewrite routes to `index.html` for React Router to work correctly.

## 📝 Environment-Specific Builds

Vite automatically uses the appropriate `.env.*` file based on the command:
- `npm run dev` → `.env.development`
- `npm run build` → `.env.production`
- `npm run preview` → `.env.production`

## 🧪 Testing

Testing configuration coming soon. Recommended setup:
- Vitest for unit tests
- React Testing Library for component tests
- Cypress or Playwright for e2e tests

## 📖 Resources

- [Vite Documentation](https://vitejs.dev)
- [React Documentation](https://react.dev)
- [TypeScript Documentation](https://www.typescriptlang.org)
- [Tailwind CSS Documentation](https://tailwindcss.com)
- [React Router Documentation](https://reactrouter.com)
- [TanStack Query Documentation](https://tanstack.com/query)

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/amazing-feature`
2. Commit changes: `git commit -m 'Add amazing feature'`
3. Push to branch: `git push origin feature/amazing-feature`
4. Open a Pull Request

## 📄 License

This project is part of the SeldonVigil platform.
