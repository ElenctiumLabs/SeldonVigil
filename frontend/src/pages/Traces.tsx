import { useState, useMemo, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  Clock,
  Activity,
  Search,
  CheckCircle2,
  XCircle,
  Copy,
  Check,
  Terminal,
  Layers,
  Sparkles,
  Code2,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { FadeIn } from '@/components/ui/animations'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { useTraces, useTraceDetails, useAgents, type Trace } from '@/hooks/useApi'
import { TraceTree } from '@/components/trace/TraceTree'
import { cn } from '@/lib/utils'

function mapTraceStatus(status: string) {
  const s = (status || '').toLowerCase()
  if (s === 'ok' || s === 'success') return 'success'
  if (s === 'error') return 'error'
  if (s === 'degraded') return 'degraded'
  return s
}

export function TracesPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  const queryAgentId = searchParams.get('agentId') || ''
  const queryStatus = searchParams.get('status') || ''
  const queryTraceId = searchParams.get('traceId') || ''

  const [searchQuery, setSearchQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState(queryStatus)
  const [agentFilter, setAgentFilter] = useState(queryAgentId)
  const [selectedTraceId, setSelectedTraceId] = useState<string | null>(queryTraceId || null)

  const urlFrom = searchParams.get('from')
  const urlTo = searchParams.get('to')
  const from = useMemo(
    () => urlFrom || new Date(Date.now() - 86400000 * 7).toISOString(),
    [urlFrom],
  )
  const to = useMemo(
    () => urlTo || new Date().toISOString(),
    [urlTo],
  )

  const { data: agents } = useAgents()
  const { data: rawTraces, isLoading } = useTraces({
    agentId: agentFilter || undefined,
    status: statusFilter || undefined,
    from,
    to,
  })

  // Synchronize URL query params
  const handleSelectTrace = (id: string) => {
    setSelectedTraceId(id)
    const newParams = new URLSearchParams(searchParams)
    newParams.set('traceId', id)
    setSearchParams(newParams, { replace: true })
  }

  const handleStatusFilterChange = (newStatus: string) => {
    setStatusFilter(newStatus)
    const newParams = new URLSearchParams(searchParams)
    if (newStatus) {
      newParams.set('status', newStatus)
    } else {
      newParams.delete('status')
    }
    setSearchParams(newParams, { replace: true })
  }

  const handleAgentFilterChange = (newAgentId: string) => {
    setAgentFilter(newAgentId)
    const newParams = new URLSearchParams(searchParams)
    if (newAgentId) {
      newParams.set('agentId', newAgentId)
    } else {
      newParams.delete('agentId')
    }
    setSearchParams(newParams, { replace: true })
  }

  // Filter traces in-memory by search term
  const filteredTraces = useMemo(() => {
    if (!rawTraces) return []
    if (!searchQuery.trim()) return rawTraces

    const q = searchQuery.toLowerCase()
    return rawTraces.filter(t =>
      t.operationName.toLowerCase().includes(q) ||
      t.agentName.toLowerCase().includes(q) ||
      t.agentId.toLowerCase().includes(q) ||
      t.id.toLowerCase().includes(q) ||
      (t.modelName && t.modelName.toLowerCase().includes(q))
    )
  }, [rawTraces, searchQuery])

  // Select first trace if none selected and traces arrive
  useEffect(() => {
    if (!selectedTraceId && filteredTraces.length > 0) {
      setSelectedTraceId(filteredTraces[0].id)
    }
  }, [filteredTraces, selectedTraceId])

  return (
    <div className="space-y-6">
      <FadeIn>
        <div className="page-header flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Trace Explorer</h1>
            <p className="page-subtitle">
              Hierarchical execution tracing and decision-tree debugging for multi-agent systems
            </p>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs font-mono text-muted-foreground">
              {filteredTraces.length} {filteredTraces.length === 1 ? 'trace' : 'traces'}
            </span>
          </div>
        </div>
      </FadeIn>

      {/* Filter Toolbar */}
      <div className="flex flex-col sm:flex-row items-center gap-3 bg-card/60 p-3 rounded-2xl border border-border backdrop-blur-sm">
        {/* Search */}
        <div className="relative flex-1 w-full">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            placeholder="Search operations, models, agents, IDs..."
            className="pl-9 bg-background/50 h-9 rounded-xl text-sm"
          />
        </div>

        {/* Agent Filter */}
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <select
            value={agentFilter}
            onChange={e => handleAgentFilterChange(e.target.value)}
            className="h-9 px-3 rounded-xl bg-background/50 border border-input text-xs font-medium text-foreground focus:outline-none focus:ring-1 focus:ring-primary w-full sm:w-44"
          >
            <option value="">All Agents</option>
            {agents?.map(a => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>

          {/* Status Filter */}
          <select
            value={statusFilter}
            onChange={e => handleStatusFilterChange(e.target.value)}
            className="h-9 px-3 rounded-xl bg-background/50 border border-input text-xs font-medium text-foreground focus:outline-none focus:ring-1 focus:ring-primary w-full sm:w-36"
          >
            <option value="">All Statuses</option>
            <option value="ok">Success / Ok</option>
            <option value="error">Error</option>
          </select>
        </div>
      </div>

      {/* Main Grid: Left = Trace List, Right = Trace Detail */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Trace List (5 cols on lg) */}
        <div className="lg:col-span-5 space-y-4">
          <Card className="rounded-2xl border-border shadow-sm">
            <CardHeader className="pb-3 border-b border-border/60">
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm font-semibold font-mono uppercase tracking-wider flex items-center gap-2">
                  <Layers className="h-4 w-4 text-primary" />
                  Runs & Spans
                </CardTitle>
                <span className="text-xs text-muted-foreground">
                  Latest first
                </span>
              </div>
            </CardHeader>
            <CardContent className="p-3">
              <TracesTable
                traces={filteredTraces}
                isLoading={isLoading}
                onSelectTrace={handleSelectTrace}
                selectedTraceId={selectedTraceId}
              />
            </CardContent>
          </Card>
        </div>

        {/* Trace Detail (7 cols on lg) */}
        <div className="lg:col-span-7">
          {selectedTraceId ? (
            <TraceDetailPanel traceId={selectedTraceId} allTraces={rawTraces || []} />
          ) : (
            <Card className="flex items-center justify-center min-h-[460px] rounded-2xl border-dashed">
              <div className="text-center text-muted-foreground p-8">
                <Activity className="h-12 w-12 mx-auto mb-4 opacity-30 text-primary" />
                <p className="text-lg font-display font-semibold text-foreground">No Trace Selected</p>
                <p className="text-sm mt-1 max-w-sm mx-auto">
                  Select a run from the left list to inspect its execution tree, token usage, and metadata.
                </p>
              </div>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}

function TracesTable({
  traces,
  isLoading,
  onSelectTrace,
  selectedTraceId,
}: {
  traces: Trace[]
  isLoading: boolean
  onSelectTrace: (id: string) => void
  selectedTraceId: string | null
}) {
  const [copiedSeed, setCopiedSeed] = useState(false)

  const copySeedCli = () => {
    navigator.clipboard.writeText('dotnet run --project src/SeldonVigil.Cli -- seed')
    setCopiedSeed(true)
    setTimeout(() => setCopiedSeed(false), 2000)
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2, 3, 4, 5].map(i => (
          <Skeleton key={i} className="h-16 w-full rounded-xl" />
        ))}
      </div>
    )
  }

  if (!traces.length) {
    return (
      <div className="text-center py-12 px-4 space-y-4">
        <div className="h-12 w-12 rounded-2xl bg-primary/10 flex items-center justify-center mx-auto text-primary">
          <Terminal className="h-6 w-6" />
        </div>
        <div>
          <p className="text-base font-semibold text-foreground">No Traces Found</p>
          <p className="text-xs text-muted-foreground mt-1 max-w-xs mx-auto">
            Seed realistic multi-agent execution traces with a single CLI command:
          </p>
        </div>
        <div className="bg-muted/50 p-2.5 rounded-xl border border-border flex items-center justify-between text-xs font-mono text-left max-w-xs mx-auto">
          <span className="truncate text-muted-foreground">$ seldonvigil seed</span>
          <Button
            size="sm"
            variant="ghost"
            className="h-7 px-2 text-xs"
            onClick={copySeedCli}
          >
            {copiedSeed ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-2 max-h-[720px] overflow-y-auto pr-1">
      {traces.map(trace => {
        const traceStatus = mapTraceStatus(trace.status)
        const isSelected = selectedTraceId === trace.id
        const isRoot = !trace.parentTraceId

        return (
          <div
            key={trace.id}
            onClick={() => onSelectTrace(trace.id)}
            className={cn(
              "p-3.5 rounded-xl border cursor-pointer transition-all duration-150",
              isSelected
                ? 'border-primary bg-primary/[0.05] shadow-sm'
                : 'border-border hover:border-primary/40 hover:bg-muted/30'
            )}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-start gap-3 min-w-0 flex-1">
                {/* Status Dot */}
                <div className={cn(
                  "w-2.5 h-2.5 rounded-full flex-shrink-0 mt-1.5",
                  traceStatus === 'success' ? 'bg-emerald-400' : traceStatus === 'degraded' ? 'bg-amber-400' : 'bg-red-400'
                )} />

                <div className="min-w-0 flex-1">
                  {/* Operation Name */}
                  <div className="flex items-center gap-2">
                    <p className="font-mono text-xs sm:text-sm font-semibold text-foreground truncate">
                      {trace.operationName}
                    </p>
                    {isRoot && (
                      <span className="px-1.5 py-0.2 rounded text-[10px] uppercase font-mono tracking-wider bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                        Root
                      </span>
                    )}
                  </div>

                  {/* Agent and Relative Info */}
                  <div className="flex items-center gap-2 text-xs text-muted-foreground mt-1">
                    <span className="font-medium text-foreground/80 truncate">
                      {trace.agentName || trace.agentId.slice(0, 8)}
                    </span>
                    {trace.modelName && trace.modelName !== 'unknown' && (
                      <span className="inline-flex items-center gap-1 font-mono text-[10px] text-muted-foreground/70">
                        &bull; {trace.modelName}
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Right: duration & cost */}
              <div className="text-right flex-shrink-0">
                <p className="text-xs font-mono font-semibold text-foreground flex items-center justify-end gap-1">
                  <Clock size={12} className="text-muted-foreground" />
                  {trace.durationMs}ms
                </p>
                <div className="flex items-center gap-1.5 justify-end mt-1 text-[11px] font-mono">
                  {trace.costUsd > 0 && (
                    <span className="text-emerald-400 font-medium">
                      ${trace.costUsd.toFixed(3)}
                    </span>
                  )}
                  {trace.tokenTotal > 0 && (
                    <span className="text-muted-foreground text-[10px]">
                      {trace.tokenTotal.toLocaleString()} tok
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}

function TraceDetailPanel({
  traceId,
  allTraces,
}: {
  traceId: string
  allTraces: Trace[]
}) {
  const { data: trace, isLoading } = useTraceDetails(traceId)
  const [activeTab, setActiveTab] = useState<'tree' | 'attributes' | 'events' | 'json'>('tree')
  const [copiedJson, setCopiedJson] = useState(false)
  const [copiedAttrKey, setCopiedAttrKey] = useState<string | null>(null)

  // Current selected span inside this detail panel (defaults to current trace)
  const [inspectedSpan, setInspectedSpan] = useState<Trace | null>(null)

  useEffect(() => {
    if (trace) {
      setInspectedSpan(trace)
    }
  }, [trace])

  const copyJson = () => {
    if (!activeData) return
    navigator.clipboard.writeText(JSON.stringify(activeData, null, 2))
    setCopiedJson(true)
    setTimeout(() => setCopiedJson(false), 2000)
  }

  const copyAttr = (k: string, v: string) => {
    navigator.clipboard.writeText(`${k}: ${v}`)
    setCopiedAttrKey(k)
    setTimeout(() => setCopiedAttrKey(null), 1500)
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Card className="rounded-2xl border-border shadow-sm">
          <CardHeader>
            <Skeleton className="h-6 w-48 rounded-lg" />
          </CardHeader>
          <CardContent className="space-y-3">
            <Skeleton className="h-32 w-full rounded-xl" />
            <Skeleton className="h-48 w-full rounded-xl" />
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!trace) {
    return null
  }

  const activeData = inspectedSpan || trace
  const traceStatus = mapTraceStatus(activeData.status)

  return (
    <div className="space-y-4 animate-fade-in">
      <Card className="rounded-2xl border-border shadow-sm overflow-hidden">
        {/* Header */}
        <CardHeader className="pb-4 border-b border-border/60 bg-muted/20">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <CardTitle className="text-base font-semibold font-mono text-foreground">
                  {activeData.operationName}
                </CardTitle>
                <span className={cn(
                  "inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-mono font-medium",
                  traceStatus === 'success'
                    ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                    : traceStatus === 'degraded'
                    ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
                    : 'bg-red-500/10 text-red-400 border border-red-500/20'
                )}>
                  {traceStatus === 'success' ? (
                    <CheckCircle2 size={12} />
                  ) : (
                    <XCircle size={12} />
                  )}
                  {activeData.status}
                </span>
              </div>
              <p className="text-xs text-muted-foreground mt-1 font-mono">
                Agent: <span className="text-foreground">{activeData.agentName}</span> ({activeData.agentId})
              </p>
            </div>

            {/* Quick Metrics */}
            <div className="flex items-center gap-2 self-start sm:self-auto font-mono text-xs">
              <div className="px-3 py-1.5 rounded-xl bg-background/80 border border-border">
                <span className="text-muted-foreground text-[10px] block">LATENCY</span>
                <span className="font-semibold text-foreground">{activeData.durationMs}ms</span>
              </div>
              <div className="px-3 py-1.5 rounded-xl bg-background/80 border border-border">
                <span className="text-muted-foreground text-[10px] block">TOKENS</span>
                <span className="font-semibold text-foreground">{activeData.tokenTotal.toLocaleString()}</span>
              </div>
              <div className="px-3 py-1.5 rounded-xl bg-background/80 border border-border">
                <span className="text-muted-foreground text-[10px] block">COST</span>
                <span className="font-semibold text-emerald-400">${activeData.costUsd.toFixed(4)}</span>
              </div>
            </div>
          </div>

          {/* Navigation Tabs */}
          <div className="flex items-center gap-2 mt-4 pt-3 border-t border-border/40">
            <button
              onClick={() => setActiveTab('tree')}
              className={cn(
                "px-3 py-1.5 rounded-xl text-xs font-medium transition-colors flex items-center gap-1.5",
                activeTab === 'tree'
                  ? 'bg-primary text-primary-foreground font-semibold shadow-sm'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/50'
              )}
            >
              <Layers size={14} />
              Decision Tree
            </button>
            <button
              onClick={() => setActiveTab('attributes')}
              className={cn(
                "px-3 py-1.5 rounded-xl text-xs font-medium transition-colors flex items-center gap-1.5",
                activeTab === 'attributes'
                  ? 'bg-primary text-primary-foreground font-semibold shadow-sm'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/50'
              )}
            >
              <Sparkles size={14} />
              Attributes ({Object.keys(activeData.attributes || {}).length})
            </button>
            <button
              onClick={() => setActiveTab('events')}
              className={cn(
                "px-3 py-1.5 rounded-xl text-xs font-medium transition-colors flex items-center gap-1.5",
                activeTab === 'events'
                  ? 'bg-primary text-primary-foreground font-semibold shadow-sm'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/50'
              )}
            >
              <Clock size={14} />
              Events ({activeData.events?.length || 0})
            </button>
            <button
              onClick={() => setActiveTab('json')}
              className={cn(
                "px-3 py-1.5 rounded-xl text-xs font-medium transition-colors flex items-center gap-1.5 ml-auto",
                activeTab === 'json'
                  ? 'bg-primary text-primary-foreground font-semibold shadow-sm'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/50'
              )}
            >
              <Code2 size={14} />
              JSON
            </button>
          </div>
        </CardHeader>

        <CardContent className="p-4">
          {/* Tab 1: Execution Tree */}
          {activeTab === 'tree' && (
            <div className="space-y-4">
              <TraceTree
                root={trace}
                traces={allTraces}
                selectedTraceId={inspectedSpan?.id || trace.id}
                onSelectTrace={(span) => setInspectedSpan(span)}
              />

              {inspectedSpan && inspectedSpan.id !== trace.id && (
                <div className="p-3 rounded-xl bg-primary/[0.04] border border-primary/20 flex items-center justify-between text-xs">
                  <div className="flex items-center gap-2">
                    <span className="text-primary font-medium">Selected Child Span:</span>
                    <span className="font-mono">{inspectedSpan.operationName}</span>
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    className="h-7 text-xs"
                    onClick={() => setInspectedSpan(trace)}
                  >
                    Reset to Root
                  </Button>
                </div>
              )}
            </div>
          )}

          {/* Tab 2: Attributes */}
          {activeTab === 'attributes' && (
            <div className="space-y-3">
              {activeData.attributes && Object.keys(activeData.attributes).length > 0 ? (
                <div className="rounded-xl border border-border overflow-hidden">
                  <table className="w-full text-left text-xs font-mono">
                    <thead className="bg-muted/50 border-b border-border text-muted-foreground">
                      <tr>
                        <th className="p-3 w-1/3">Key</th>
                        <th className="p-3">Value</th>
                        <th className="p-3 w-12 text-right"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border/60">
                      {Object.entries(activeData.attributes).map(([key, val]) => (
                        <tr key={key} className="hover:bg-muted/30 transition-colors">
                          <td className="p-3 text-primary font-semibold truncate">{key}</td>
                          <td className="p-3 text-foreground break-all">{val}</td>
                          <td className="p-3 text-right">
                            <button
                              onClick={() => copyAttr(key, val)}
                              className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                              title="Copy attribute"
                            >
                              {copiedAttrKey === key ? (
                                <Check size={12} className="text-emerald-400" />
                              ) : (
                                <Copy size={12} />
                              )}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="text-center py-8 text-xs text-muted-foreground">
                  No attributes recorded for this span.
                </p>
              )}
            </div>
          )}

          {/* Tab 3: Events Timeline */}
          {activeTab === 'events' && (
            <div className="space-y-3">
              {activeData.events && activeData.events.length > 0 ? (
                <div className="relative border-l border-border/80 ml-4 pl-4 space-y-4 py-2">
                  {activeData.events.map((evt, idx) => (
                    <div key={idx} className="relative group">
                      <div className="absolute -left-[21px] top-1 h-2.5 w-2.5 rounded-full bg-primary ring-4 ring-background" />
                      <div className="p-3 rounded-xl bg-muted/40 border border-border/60 space-y-1">
                        <div className="flex items-center justify-between">
                          <span className="font-mono text-xs font-semibold text-foreground">
                            {evt.name}
                          </span>
                        </div>
                        {evt.value && (
                          <p className="font-mono text-xs text-muted-foreground break-all mt-1">
                            {evt.value}
                          </p>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-center py-8 text-xs text-muted-foreground">
                  No events recorded for this span.
                </p>
              )}
            </div>
          )}

          {/* Tab 4: Raw JSON */}
          {activeTab === 'json' && (
            <div className="relative">
              <Button
                size="sm"
                variant="outline"
                className="absolute right-3 top-3 h-7 text-xs flex items-center gap-1.5 z-10"
                onClick={copyJson}
              >
                {copiedJson ? <Check className="h-3 w-3 text-emerald-400" /> : <Copy className="h-3 w-3" />}
                {copiedJson ? 'Copied' : 'Copy JSON'}
              </Button>
              <pre className="p-4 rounded-xl bg-black/60 border border-border font-mono text-xs text-muted-foreground overflow-x-auto max-h-[500px]">
                {JSON.stringify(activeData, null, 2)}
              </pre>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}