import { useState, useMemo } from 'react'
import { DollarSign, TrendingUp, TrendingDown, Calendar, Download, ArrowUpRight } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { DataTable, type Column } from '@/components/ui/data-table'
import { Badge } from '@/components/ui/badge'
import { CostTrendChart } from '@/components/charts/CostTrendChart'
import { Skeleton } from '@/components/ui/skeleton'
import { PageLoader } from '@/components/ui/page-loader'
import { useAgentCosts, useCostTrend, useBudgets, createBudget, type AgentCostApi } from '@/hooks/useApi'
import { useQueryClient } from '@tanstack/react-query'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Plus } from 'lucide-react'
import { useToast } from '@/components/ui/toast'
import { cn } from '@/lib/utils'
import { FadeIn, CountUp } from '@/components/ui/animations'

// Extended interface to match API response with additional UI fields
interface AgentCost {
  agent: string
  cost: number
  tokens: number
  calls: number
  avgLatency: number
  trend: 'up' | 'down'
}

// Map API agent cost to UI format
function mapAgentCost(apiCost: AgentCostApi): AgentCost {
  return {
    agent: apiCost.agent || 'Unknown Agent',
    cost: apiCost.cost || 0,
    tokens: apiCost.tokens || 0,
    calls: apiCost.calls || 0,
    avgLatency: apiCost.avgLatency || 0,
    trend: apiCost.trend || 'down',
  }
}

// Real period-over-period cost delta from the daily trend series:
// compares the second half of the window against the first half.
// Returns null when the series is too short to compare or has no
// baseline spend, so callers can hide the badge instead of faking one.
function computeTrendDelta(series: { cost: number }[]): number | null {
  if (series.length < 4) return null
  const mid = Math.floor(series.length / 2)
  const prev = series.slice(0, mid).reduce((s, d) => s + d.cost, 0)
  const curr = series.slice(mid).reduce((s, d) => s + d.cost, 0)
  if (prev <= 0) return null
  return ((curr - prev) / prev) * 100
}

// Trend badge driven by a computed percentage; renders nothing when no
// reliable delta exists (rather than showing a hardcoded number).
function TrendBadge({ delta }: { delta: number | null }) {
  if (delta === null) return null
  const increased = delta > 0
  return (
    <div className={cn(
      "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium",
      increased
        ? "bg-red-500/10 text-red-500 border border-red-500/20"
        : "bg-emerald-500/10 text-emerald-500 border border-emerald-500/20"
    )}>
      {increased ? <TrendingUp size={14} /> : <TrendingDown size={14} />}
      <span>{Math.abs(delta).toFixed(1)}% vs previous period</span>
    </div>
  )
}

export function CostsPage() {
  const [selectedAgent, setSelectedAgent] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<'all' | 'highest' | 'increasing'>('all')
  const queryClient = useQueryClient()
  const { toast } = useToast()

  // Budgets
  const { data: budgets, isLoading: budgetsLoading } = useBudgets()
  const [showBudgetForm, setShowBudgetForm] = useState(false)
  const [budgetName, setBudgetName] = useState('')
  const [budgetScopeType, setBudgetScopeType] = useState<'Tenant' | 'Agent'>('Agent')
  const [budgetScopeId, setBudgetScopeId] = useState('b1bcbe9b-da35-48b0-b22e-bbb1e1fbeba9')
  const [budgetAmount, setBudgetAmount] = useState('100')
  const [budgetPeriod, setBudgetPeriod] = useState<'Monthly' | 'Weekly' | 'Daily' | 'Quarterly' | 'Annual'>('Monthly')
  const [creatingBudget, setCreatingBudget] = useState(false)

  const handleCreateBudget = async () => {
    if (!budgetName.trim() || !budgetScopeId.trim() || !Number(budgetAmount)) return
    setCreatingBudget(true)
    try {
      await createBudget({
        name: budgetName.trim(),
        scopeType: budgetScopeType,
        scopeId: budgetScopeId.trim(),
        amount: Number(budgetAmount),
        period: budgetPeriod,
      })
      await queryClient.invalidateQueries({ queryKey: ['costBudgets'] })
      await queryClient.invalidateQueries({ queryKey: ['agentCosts'] })
      toast({ type: 'success', title: 'Budget created', message: `${budgetName} is now tracking spend.` })
      setBudgetName('')
      setShowBudgetForm(false)
    } catch (err) {
      toast({ type: 'error', title: 'Create failed', message: String(err) })
    } finally {
      setCreatingBudget(false)
    }
  }


  // Fetch agent costs from API
  const { data: apiAgentCosts, isLoading, error } = useAgentCosts()

  // Fetch daily cost trend for real period-over-period comparison
  const { data: dailyTrend } = useCostTrend({ days: 30 })
  const costDeltaPct = useMemo(() => computeTrendDelta(dailyTrend ?? []), [dailyTrend])

  // Map API data to UI format
  const agentCosts = useMemo(() => {
    return apiAgentCosts?.map(mapAgentCost) || []
  }, [apiAgentCosts])

  // Calculate totals
  const totalCost = useMemo(() => agentCosts.reduce((s, a) => s + a.cost, 0), [agentCosts])
  const totalTokens = useMemo(() => agentCosts.reduce((s, a) => s + a.tokens, 0), [agentCosts])
  const totalCalls = useMemo(() => agentCosts.reduce((s, a) => s + a.calls, 0), [agentCosts])

  // Filter data based on active tab
  const filteredData = useMemo(() => {
    if (activeTab === 'highest') {
      return [...agentCosts].sort((a, b) => b.cost - a.cost).slice(0, 5)
    }
    if (activeTab === 'increasing') {
      return agentCosts.filter(a => a.trend === 'up')
    }
    return agentCosts
  }, [agentCosts, activeTab])

  const columns: Column<AgentCost>[] = [
    {
      key: 'agent',
      header: 'Agent',
      cell: (a) => <span className="font-medium">{a.agent}</span>,
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.agent.localeCompare(b.agent) : b.agent.localeCompare(a.agent),
    },
    {
      key: 'cost',
      header: 'Cost',
      cell: (a) => (
        <div className="flex items-center gap-2">
          <span className="font-mono font-semibold">£{a.cost.toFixed(2)}</span>
          {a.trend === 'up' ? (
            <TrendingUp size={14} className="text-red-500" />
          ) : (
            <TrendingDown size={14} className="text-emerald-500" />
          )}
        </div>
      ),
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.cost - b.cost : b.cost - a.cost,
      className: 'w-40',
    },
    {
      key: 'tokens',
      header: 'Tokens',
      cell: (a) => <span className="font-mono text-sm">{a.tokens.toLocaleString()}</span>,
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.tokens - b.tokens : b.tokens - a.tokens,
      className: 'w-36',
    },
    {
      key: 'calls',
      header: 'API Calls',
      cell: (a) => <span className="font-mono text-sm">{a.calls.toLocaleString()}</span>,
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.calls - b.calls : b.calls - a.calls,
      className: 'w-28',
    },
    {
      key: 'avgLatency',
      header: 'Avg Latency',
      cell: (a) => (
        <div className="flex items-center gap-1.5">
          <span className="font-mono text-sm">{a.avgLatency}ms</span>
          <div className={cn(
            "h-1.5 rounded-full w-16 bg-muted overflow-hidden",
          )}>
            <div
              className={cn(
                "h-full rounded-full",
                a.avgLatency < 1000 ? 'bg-emerald-500' :
                a.avgLatency < 2000 ? 'bg-amber-500' : 'bg-red-500'
              )}
              style={{ width: `${Math.min((a.avgLatency / 5000) * 100, 100)}%` }}
            />
          </div>
        </div>
      ),
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.avgLatency - b.avgLatency : b.avgLatency - a.avgLatency,
      className: 'w-48',
    },
    {
      key: 'costPerToken',
      header: 'Cost/1K Tokens',
      cell: (a) => {
        const cpt = (a.cost / a.tokens) * 1000
        return (
          <span className="font-mono text-sm">£{cpt.toFixed(4)}</span>
        )
      },
      sortable: true,
      sortFn: (a, b, dir) => {
        const aCpt = (a.cost / a.tokens) * 1000
        const bCpt = (b.cost / b.tokens) * 1000
        return dir === 'asc' ? aCpt - bCpt : bCpt - aCpt
      },
      className: 'w-32',
    },
  ]

  // Loading state
  if (isLoading) {
    return <PageLoader label="Loading cost intelligence" />
  }

  // Error state
  if (error) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Cost Intelligence</h1>
            <p className="page-subtitle">Track and optimize costs across all agents and services</p>
          </div>
        </div>
        <div className="text-center py-12">
          <p className="text-destructive">Failed to load cost data</p>
          <p className="text-sm text-muted-foreground mt-2">Please check your backend connection</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <FadeIn>
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Cost Intelligence</h1>
            <p className="page-subtitle">
              Track and optimize costs across all agents and services
            </p>
          </div>
        </div>
      </FadeIn>

      {/* Cost Overview Cards */}
      <FadeIn delay={0.1}>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
          <div className="stat-card">
            <div className="flex items-start justify-between">
              <div className="space-y-2">
                <p className="text-sm font-medium text-muted-foreground">Total Cost (30d)</p>
                <p className="text-3xl font-display font-bold">£<CountUp value={totalCost} decimals={2} /></p>
                <TrendBadge delta={costDeltaPct} />
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
                <p className="text-3xl font-display font-bold"><CountUp value={totalTokens / 1000000} decimals={1} />M</p>
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
                <p className="text-3xl font-display font-bold"><CountUp value={totalCalls} decimals={0} /></p>
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
                <p className="text-3xl font-display font-bold">£<CountUp value={totalCalls > 0 ? totalCost / totalCalls : 0} decimals={4} /></p>
              </div>
              <div className="stat-icon-container flex-shrink-0">
                <TrendingDown size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
            </div>
          </div>
        </div>
      </FadeIn>

      {/* Budgets ── create + list */}
      <FadeIn delay={0.15}>
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <DollarSign size={20} className="text-primary" />
                <CardTitle className="font-display">Budgets</CardTitle>
              </div>
              <Button size="sm" onClick={() => setShowBudgetForm((s) => !s)}>
                <Plus className="h-4 w-4 mr-1" /> New Budget
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {showBudgetForm && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 items-end rounded-xl border border-border/40 bg-muted/30 p-4">
                <div className="space-y-1.5">
                  <Label htmlFor="b-name">Name</Label>
                  <Input id="b-name" value={budgetName} onChange={(e) => setBudgetName(e.target.value)} placeholder="SeldonVigil monthly" />
                </div>
                <div className="space-y-1.5">
                  <Label>Scope</Label>
                  <Select
                    value={budgetScopeType}
                    onValueChange={(v) => setBudgetScopeType(v as 'Tenant' | 'Agent')}
                    options={[
                      { value: 'Agent', label: 'Agent' },
                      { value: 'Tenant', label: 'Tenant' },
                    ]}
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="b-scope">Scope ID (GUID)</Label>
                  <Input id="b-scope" value={budgetScopeId} onChange={(e) => setBudgetScopeId(e.target.value)} className="font-mono text-xs" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="b-amount">Amount (USD)</Label>
                  <Input id="b-amount" type="number" min="0" step="1" value={budgetAmount} onChange={(e) => setBudgetAmount(e.target.value)} />
                </div>
                <div className="space-y-1.5">
                  <Label>Period</Label>
                  <Select
                    value={budgetPeriod}
                    onValueChange={(v) => setBudgetPeriod(v as typeof budgetPeriod)}
                    options={[
                      { value: 'Monthly', label: 'Monthly' },
                      { value: 'Weekly', label: 'Weekly' },
                      { value: 'Daily', label: 'Daily' },
                      { value: 'Quarterly', label: 'Quarterly' },
                      { value: 'Annual', label: 'Annual' },
                    ]}
                  />
                </div>
                <div className="flex items-center gap-2 lg:col-span-2">
                  <Button onClick={handleCreateBudget} disabled={creatingBudget || !budgetName.trim() || !Number(budgetAmount)}>
                    {creatingBudget ? 'Creating…' : 'Create Budget'}
                  </Button>
                  <Button variant="ghost" onClick={() => setShowBudgetForm(false)}>Cancel</Button>
                </div>
              </div>
            )}

            {budgetsLoading ? (
              <Skeleton className="h-16 w-full rounded-xl" />
            ) : !budgets || budgets.length === 0 ? (
              <p className="text-sm text-muted-foreground py-2">No budgets yet — create one to start tracking spend against limits.</p>
            ) : (
              <div className="space-y-3">
                {budgets.map((b) => {
                  const util = Math.min(100, b.utilizationPercent)
                  return (
                    <div key={b.id?.value ?? b.name} className="rounded-xl border border-border/40 p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <span className="font-medium text-sm">{b.name}</span>
                          <Badge variant={b.status === 'Exceeded' ? 'destructive' : util >= 80 ? 'warning' : 'success'}>
                            {util.toFixed(0)}% used
                          </Badge>
                        </div>
                        <span className="text-sm text-muted-foreground font-mono">
                          ${b.currentSpend.amount.toFixed(2)} / ${b.budgetAmount.amount.toFixed(2)}
                        </span>
                      </div>
                      <div className="h-2 rounded-full bg-muted overflow-hidden">
                        <div
                          className={cn('h-full rounded-full transition-all', util >= 100 ? 'bg-red-500' : util >= 80 ? 'bg-amber-500' : 'bg-emerald-500')}
                          style={{ width: `${util}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>
      </FadeIn>
      {/* Cost Trend Chart */}
      <CostTrendChart days={30} showBudget budgetAmount={5000} />

      {/* Agent Cost Breakdown */}
      <Tabs defaultValue="all" value={activeTab} onValueChange={(v) => setActiveTab(v as 'all' | 'highest' | 'increasing')}>
        <div className="flex items-center justify-between mb-4">
          <TabsList>
            <TabsTrigger value="all">All Agents</TabsTrigger>
            <TabsTrigger value="highest">Highest Cost</TabsTrigger>
            <TabsTrigger value="increasing">Increasing</TabsTrigger>
          </TabsList>
          <Button variant="outline" size="sm" className="rounded-xl h-9">
            <Download className="h-4 w-4 mr-2" />
            Export Report
          </Button>
        </div>

        <TabsContent value="all">
          <DataTable
            data={filteredData}
            columns={columns}
            keyExtractor={(a) => a.agent}
            pageSize={10}
            onRowClick={(a) => setSelectedAgent(a.agent === selectedAgent ? null : a.agent)}
            selectedKey={selectedAgent}
          />
        </TabsContent>
        <TabsContent value="highest">
          <DataTable
            data={filteredData}
            columns={columns}
            keyExtractor={(a) => a.agent}
            pageSize={5}
            onRowClick={(a) => setSelectedAgent(a.agent === selectedAgent ? null : a.agent)}
            selectedKey={selectedAgent}
          />
        </TabsContent>
        <TabsContent value="increasing">
          <DataTable
            data={filteredData}
            columns={columns}
            keyExtractor={(a) => a.agent}
            pageSize={10}
            onRowClick={(a) => setSelectedAgent(a.agent === selectedAgent ? null : a.agent)}
            selectedKey={selectedAgent}
          />
        </TabsContent>
      </Tabs>

      {/* Selected Agent Detail */}
      {selectedAgent && (
        <Card className="rounded-2xl border-border shadow-sm animate-fade-in">
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base font-semibold">{selectedAgent}</CardTitle>
              <Badge variant="success">Active</Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <CostTrendChart agentId={selectedAgent} days={30} />
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
