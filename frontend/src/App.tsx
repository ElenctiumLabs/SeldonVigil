import { useEffect } from 'react'
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@/hooks/useTheme'
import { ToastProvider } from '@/components/ui/toast'
import { AuthProvider } from '@/hooks/useAuth'
import { LayoutProvider } from '@/hooks/useLayout'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { TracesPage } from '@/pages/Traces'
import { SecurityPage } from '@/pages/Security'
import { CostsPage } from '@/pages/Costs'
import { SettingsPage } from '@/pages/Settings'
import { BrokerPage } from '@/pages/Broker'
import { LoginPage } from '@/pages/Login'
import { GovernancePage } from '@/pages/Governance'
import { CertificationPage } from '@/pages/Certification'
import { IntegrationsPage } from '@/pages/Integrations'
import { AIReasoningPage } from '@/pages/AIReasoning'
import { AgentsPage } from '@/pages/Agents'
import { DocsPage } from '@/pages/DocsPage'
import {
  Activity,
  ArrowUpRight,
  Calendar,
  Cpu,
  DollarSign,
  Shield,
  TrendingUp,
  TrendingDown,
  LucideIcon
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { Skeleton } from '@/components/ui/skeleton'
import { useHealthStatus, useAgents, useSecurityViolations, useCostTrend, useAgentCosts, useTraces } from '@/hooks/useApi'
import { FadeIn, Stagger, CountUp } from '@/components/ui/animations'
import { CostTrendChart } from '@/components/charts/CostTrendChart'
import { motion } from 'framer-motion'
import './App.css'

/** The docs are a standalone viewer served by nginx under /docs. */
function RedirectToDocs() {
  useEffect(() => {
    window.location.replace('/docs')
  }, [])
  return null
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      gcTime: 1000 * 60 * 10,
    },
  },
})

interface StatCardProps {
  title: string
  value: string | number
  change?: string
  changeType?: 'up' | 'down'
  icon: LucideIcon
  label?: string
  isLoading?: boolean
}

function StatCard({ title, value, change, changeType, icon: Icon, label, isLoading }: StatCardProps) {
  if (isLoading) {
    return (
      <div className="stat-card-branded">
        <Skeleton className="h-24 w-full rounded-xl" />
      </div>
    )
  }

  const isNumeric = typeof value === 'number'
  const isCurrency = typeof value === 'string' && (value.includes('£') || value.includes('$'))
  const currencySymbol = isCurrency ? value.charAt(0) : ''
  const numericPart = isCurrency ? parseFloat(value.slice(1)) : 0

  return (
    <div className="stat-card-branded">
      <div className="flex items-start justify-between gap-4">
        {/* Left: label + value + change badge */}
        <div className="flex flex-col gap-2 min-w-0">
          {/* Label — Fira Code mono, muted, uppercase */}
          <p
            className="text-muted-foreground uppercase tracking-widest font-medium"
            style={{ fontFamily: "'Fira Code', monospace", fontSize: '0.75rem' }}
          >
            {title}
          </p>

          {/* Value — Cormorant SC, gold shimmer */}
          <p
            className="text-gold-shimmer font-semibold leading-none"
            style={{
              fontSize: 'clamp(1.75rem, 2.4vw, 2.25rem)',
            }}
          >
            {isNumeric ? (
              <CountUp value={value as number} decimals={0} />
            ) : isCurrency ? (
              <span>{currencySymbol}<CountUp value={numericPart} decimals={2} /></span>
            ) : (
              value
            )}
          </p>

          {/* Change badge */}
          {change && (
            <div className={cn(
              "inline-flex items-center gap-1 self-start px-2 py-0.5 rounded-full text-xs font-medium border",
              changeType === 'up'
                ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/25'
                : 'bg-red-500/10 text-red-400 border-red-500/25'
            )}
            style={{ fontFamily: "'Fira Code', monospace", fontSize: '0.7rem' }}
            >
              {changeType === 'up' ? <TrendingUp size={11} /> : <TrendingDown size={11} />}
              <span>{change}</span>
            </div>
          )}

          {/* Sub-label */}
          {label && (
            <p
              className="text-muted-foreground"
              style={{ fontFamily: "'Fira Code', monospace", fontSize: '0.7rem', opacity: 0.6 }}
            >
              {label}
            </p>
          )}
        </div>

        {/* Right: alchemical icon container — emerald ring, glow on hover */}
        <div className="stat-icon-container flex-shrink-0">
          <Icon size={22} className="text-emerald-400" strokeWidth={1.5} />
        </div>
      </div>
    </div>
  )
}

function Dashboard() {
  // Fetch real data from APIs
  const { data: agents, isLoading: agentsLoading } = useAgents()
  const { data: traces, isLoading: tracesLoading } = useTraces({ limit: 100 })
  const { data: violations, isLoading: violationsLoading } = useSecurityViolations()
  const { data: costTrend, isLoading: costLoading } = useCostTrend({ days: 30 })
  const { data: agentCosts, isLoading: agentCostsLoading } = useAgentCosts()

  // Calculate stats from real data
  const totalAgents = agents?.length || 0
  const activeTraces = traces?.filter(t => t.status === 'success').length || 0
  const securityEvents = violations?.length || 0
  const totalCost = Array.isArray(costTrend) ? costTrend.reduce((sum, d) => sum + d.cost, 0) : 0

  // Live cost metrics mirrored from /costs/dashboard (real SeldonVigil telemetry)
  const costMetrics = (agentCosts ?? []).reduce(
    (acc, a) => ({
      cost: acc.cost + (a.cost || 0),
      tokens: acc.tokens + (a.tokens || 0),
      calls: acc.calls + (a.calls || 0),
    }),
    { cost: 0, tokens: 0, calls: 0 }
  )
  const avgCostPerCall = costMetrics.calls > 0 ? costMetrics.cost / costMetrics.calls : 0
  const costLoadingAny = costLoading || agentCostsLoading

  const stats: StatCardProps[] = [
    {
      title: 'Total Agents',
      value: totalAgents,
      icon: Cpu,
      label: 'registered',
    },
    {
      title: 'Active Traces',
      value: activeTraces,
      icon: Activity,
      label: 'succeeded',
    },
    {
      title: 'Security Events',
      value: securityEvents,
      icon: Shield,
      label: securityEvents === 1 ? 'violation' : 'violations',
    },
    {
      title: 'Total Cost',
      value: `\u00A3${totalCost.toFixed(2)}`,
      icon: DollarSign,
      label: 'last 30 days',
    },
  ]

  const isLoading = agentsLoading || tracesLoading || violationsLoading || costLoading

  return (
    <div className="space-y-8">
      {/* Page Header */}
      <FadeIn>
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Dashboard</h1>
            <p className="page-subtitle">
              Enterprise-grade Observability, Security, and Governance
            </p>
          </div>
        </div>
      </FadeIn>

      {/* Stats Grid */}
      <Stagger staggerDelay={0.1} className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
        {stats.map((stat) => (
          <StatCard key={stat.title} {...stat} isLoading={isLoading} />
        ))}
      </Stagger>

      {/* Live Cost Overview — mirrors /costs so real spend stays on the Dashboard */}
      <Stagger staggerDelay={0.1} className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-display font-semibold text-foreground">Cost Overview</h3>
          <Link
            to="/costs"
            className="text-sm font-medium text-primary hover:text-primary/80 transition-colors"
          >
            View Costs
          </Link>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
          <div className="stat-card">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <p className="text-sm font-medium text-muted-foreground">Total Cost (30d)</p>
                {costLoadingAny ? (
                  <Skeleton className="h-9 w-28 rounded-lg" />
                ) : (
                  <p className="text-3xl font-display font-bold">£<CountUp value={costMetrics.cost} decimals={2} /></p>
                )}
              </div>
              <div className="stat-icon-container flex-shrink-0">
                <DollarSign size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
            </div>
          </div>
          <div className="stat-card">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <p className="text-sm font-medium text-muted-foreground">Total Tokens</p>
                {costLoadingAny ? (
                  <Skeleton className="h-9 w-28 rounded-lg" />
                ) : (
                  <p className="text-3xl font-display font-bold"><CountUp value={costMetrics.tokens / 1000000} decimals={1} />M</p>
                )}
              </div>
              <div className="stat-icon-container flex-shrink-0">
                <ArrowUpRight size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
            </div>
          </div>
          <div className="stat-card">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <p className="text-sm font-medium text-muted-foreground">API Calls</p>
                {costLoadingAny ? (
                  <Skeleton className="h-9 w-28 rounded-lg" />
                ) : (
                  <p className="text-3xl font-display font-bold"><CountUp value={costMetrics.calls} decimals={0} /></p>
                )}
              </div>
              <div className="stat-icon-container flex-shrink-0">
                <Calendar size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
            </div>
          </div>
          <div className="stat-card">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <p className="text-sm font-medium text-muted-foreground">Avg Cost/Call</p>
                {costLoadingAny ? (
                  <Skeleton className="h-9 w-28 rounded-lg" />
                ) : (
                  <p className="text-3xl font-display font-bold">£<CountUp value={avgCostPerCall} decimals={4} /></p>
                )}
              </div>
              <div className="stat-icon-container flex-shrink-0">
                <TrendingDown size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
            </div>
          </div>
        </div>
      </Stagger>

      {/* Quick Actions & Activity */}
      <Stagger staggerDelay={0.15} className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        {/* Recent Activity - Using Traces as Activity */}
        <div className="lg:col-span-2 stat-card glass-card">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-lg font-display font-semibold text-foreground">Recent Activity</h3>
            <Link
              to="/traces"
              className="text-sm font-medium text-primary hover:text-primary/80 transition-colors"
            >
              View All
            </Link>
          </div>
          <div className="space-y-4">
            {tracesLoading ? (
              <div className="space-y-3">
                {[1, 2, 3, 4].map(i => (
                  <Skeleton key={i} className="h-12 w-full rounded-xl" />
                ))}
              </div>
            ) : traces && traces.length > 0 ? (
              traces.slice(0, 4).map(trace => (
                <Link
                  key={trace.id}
                  to={`/traces?traceId=${trace.id}`}
                  className="flex items-center gap-4 p-3 rounded-xl hover:bg-white/5 transition-colors border border-transparent hover:border-white/[0.06] group cursor-pointer"
                >
                  <div className={cn(
                    "w-2.5 h-2.5 rounded-full flex-shrink-0",
                    trace.status === 'success' ? 'bg-emerald-500' : 'bg-red-500'
                  )} />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-mono font-medium text-foreground truncate group-hover:text-primary transition-colors">
                      {trace.operationName || trace.id}
                    </p>
                    <p className="text-xs text-muted-foreground mt-0.5 truncate">
                      {trace.agentName || trace.agentId.slice(0, 8)} &bull; {trace.status}
                    </p>
                  </div>
                  <span className="text-xs font-mono text-muted-foreground flex-shrink-0">{trace.durationMs}ms</span>
                </Link>
              ))
            ) : (
              <p className="text-sm text-muted-foreground">No recent activity</p>
            )}
          </div>
        </div>

        {/* System Health */}
        <SystemHealthCard />
      </Stagger>

      {/* Cost Trend */}
      <FadeIn delay={0.05}>
        <CostTrendChart days={30} />
      </FadeIn>
    </div>
  )
}

function SystemHealthCard() {
  const { data: health, isLoading, error } = useHealthStatus()

  if (isLoading) {
    return (
      <div className="stat-card glass-card">
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    )
  }

  if (error || !health) {
    return (
      <div className="stat-card glass-card">
        <p className="text-sm text-muted-foreground">Health data unavailable</p>
      </div>
    )
  }

  const healthyCount = health.services.filter(s => s.status === 'healthy').length
  const totalCount = health.services.length
  const healthPercentage = totalCount > 0 ? Math.round((healthyCount / totalCount) * 100) : 0
  const avgDuration = health.services.length > 0
    ? Math.round(health.services.reduce((acc, _) => acc, 0) / health.services.length)
    : 0

  const metrics = [
    {
      label: 'Overall Status',
      value: health.status === 'healthy' ? 'Operational' : health.status === 'degraded' ? 'Degraded' : 'Down',
      percentage: health.status === 'healthy' ? 100 : health.status === 'degraded' ? 50 : 0,
      color: health.status === 'healthy' ? 'bg-emerald-500' : health.status === 'degraded' ? 'bg-yellow-500' : 'bg-red-500',
    },
    {
      label: 'Services Healthy',
      value: `${healthyCount}/${totalCount}`,
      percentage: healthPercentage,
      color: healthPercentage >= 80 ? 'bg-emerald-500' : healthPercentage >= 50 ? 'bg-yellow-500' : 'bg-red-500',
    },
    {
      label: 'Avg Check Duration',
      value: `${avgDuration}ms`,
      percentage: avgDuration < 1000 ? 95 : avgDuration < 5000 ? 70 : 30,
      color: avgDuration < 1000 ? 'bg-emerald-500' : avgDuration < 5000 ? 'bg-yellow-500' : 'bg-red-500',
    },
  ]

  return (
    <div className="stat-card glass-card">
      <h3 className="text-lg font-display font-semibold text-foreground mb-6">System Health</h3>
      <div className="space-y-5">
        {metrics.map((metric) => (
          <div key={metric.label}>
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-muted-foreground">{metric.label}</span>
              <span className="text-sm font-semibold text-foreground">{metric.value}</span>
            </div>
            <div className="h-2 bg-muted rounded-full overflow-hidden">
              <motion.div
                className={cn("h-full rounded-full", metric.color)}
                initial={{ width: 0 }}
                animate={{ width: `${metric.percentage}%` }}
                transition={{ duration: 1, delay: 0.5 }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <ToastProvider>
          <AuthProvider>
            <LayoutProvider>
            <Router>
              <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/" element={
                  <ProtectedRoute>
                    <DashboardLayout />
                  </ProtectedRoute>
                }>
                  <Route index element={<Dashboard />} />
                  <Route path="traces" element={<TracesPage />} />
                  <Route path="security" element={<SecurityPage />} />
                  <Route path="costs" element={<CostsPage />} />
                  <Route path="broker" element={<BrokerPage />} />
                  <Route path="governance" element={<GovernancePage />} />
                  <Route path="governance/certification" element={<CertificationPage />} />
                  <Route path="integrations" element={<IntegrationsPage />} />
                  <Route path="ai-reasoning" element={<AIReasoningPage />} />
                  <Route path="docs" element={<DocsPage />} />
                  <Route path="reference" element={<RedirectToDocs />} />
                  <Route path="agents" element={<AgentsPage />} />
                  <Route path="settings" element={<SettingsPage />} />
                </Route>
              </Routes>
            </Router>
            </LayoutProvider>
          </AuthProvider>
        </ToastProvider>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App
