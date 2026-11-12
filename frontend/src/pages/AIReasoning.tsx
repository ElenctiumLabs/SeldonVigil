import { useState } from 'react'
import { Brain, Play, ChevronDown, ChevronRight, AlertTriangle, Lightbulb, Target, Clock } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { FadeIn } from '@/components/ui/animations'
import { useTriggerRca, useRcaHistory, type RootCauseAnalysis } from '@/hooks/useApi'

function ConfidenceBar({ confidence, label }: { confidence: number; label: string }) {
  const color = confidence >= 0.7 ? 'bg-green-500' : confidence >= 0.4 ? 'bg-yellow-500' : 'bg-red-500'
  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between text-sm">
        <span className="text-muted-foreground">Confidence</span>
        <span className="font-medium">{label} ({(confidence * 100).toFixed(0)}%)</span>
      </div>
      <div className="h-2 bg-muted rounded-full overflow-hidden">
        <div className={`h-full rounded-full ${color}`} style={{ width: `${confidence * 100}%` }} />
      </div>
    </div>
  )
}

function RcaCard({ rca }: { rca: RootCauseAnalysis }) {
  const [expanded, setExpanded] = useState(false)

  return (
    <Card className="glass-card">
      <CardContent className="p-5">
        <div className="flex items-start justify-between cursor-pointer" onClick={() => setExpanded(!expanded)}>
          <div className="flex-1">
            <div className="flex items-center gap-3 mb-2">
              <h3 className="font-semibold text-foreground">{rca.anomalyType}</h3>
              <Badge variant={rca.status === 'completed' ? 'default' : rca.status === 'failed' ? 'destructive' : 'secondary'}>
                {rca.status}
              </Badge>
              <span className="text-xs text-muted-foreground">Agent</span>
            </div>
            <p className="text-sm text-muted-foreground line-clamp-2">{rca.probableRootCause}</p>
          </div>
          {expanded ? <ChevronDown className="w-5 h-5 text-muted-foreground" /> : <ChevronRight className="w-5 h-5 text-muted-foreground" />}
        </div>

        {expanded && (
          <div className="mt-4 space-y-4 border-t pt-4">
            <ConfidenceBar confidence={rca.confidence} label={rca.confidenceLabel} />

            <div>
              <h4 className="text-sm font-medium text-foreground mb-2 flex items-center gap-2">
                <Target className="w-4 h-4" /> Root Cause
              </h4>
              <p className="text-sm text-muted-foreground">{rca.probableRootCause}</p>
            </div>

            {rca.evidence.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-foreground mb-2">Evidence</h4>
                <ul className="space-y-1">
                  {rca.evidence.map((e, i) => (
                    <li key={i} className="text-sm text-muted-foreground flex items-start gap-2">
                      <span className="text-primary mt-0.5">•</span> {e}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {rca.contributingFactors.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-foreground mb-2 flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4" /> Contributing Factors
                </h4>
                <ul className="space-y-1">
                  {rca.contributingFactors.map((f, i) => (
                    <li key={i} className="text-sm text-muted-foreground flex items-start gap-2">
                      <span className="text-amber-500 mt-0.5">•</span> {f}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {rca.recommendedActions.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-foreground mb-2 flex items-center gap-2">
                  <Lightbulb className="w-4 h-4" /> Recommended Actions
                </h4>
                <div className="space-y-2">
                  {rca.recommendedActions.map((a, i) => (
                    <div key={i} className="flex items-center gap-3 p-2 rounded-md bg-muted/50">
                      <Badge variant={a.priority === 'P0' ? 'destructive' : a.priority === 'P1' ? 'warning' : 'secondary'} className="text-xs">
                        {a.priority}
                      </Badge>
                      <span className="text-sm text-foreground flex-1">{a.action}</span>
                      {a.owner && <span className="text-xs text-muted-foreground">{a.owner}</span>}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex items-center gap-4 text-xs text-muted-foreground pt-2 border-t">
              <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> {new Date(rca.createdAt).toLocaleString()}</span>
              <span>Anomaly: {new Date(rca.anomalyStartTime).toLocaleString()} — {new Date(rca.anomalyEndTime).toLocaleString()}</span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

export function AIReasoningPage() {
  const { data: history, isLoading } = useRcaHistory()
  const triggerRca = useTriggerRca()
  const [form, setForm] = useState({ agentId: '', anomalyType: '', traceId: '' })
  const [showForm, setShowForm] = useState(false)

  const handleTrigger = async () => {
    if (!form.agentId || !form.anomalyType) return
    const now = new Date()
    const hourAgo = new Date(now.getTime() - 60 * 60 * 1000)
    await triggerRca.mutateAsync({
      agentId: form.agentId,
      anomalyType: form.anomalyType,
      anomalyStartTime: hourAgo.toISOString(),
      anomalyEndTime: now.toISOString(),
      traceId: form.traceId || undefined,
    })
    setShowForm(false)
    setForm({ agentId: '', anomalyType: '', traceId: '' })
  }

  return (
    <FadeIn>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">AI Reasoning</h1>
            <p className="page-subtitle">AI-powered root cause analysis for agent anomalies</p>
          </div>
          <Button onClick={() => setShowForm(!showForm)}>
            <Brain className="w-4 h-4 mr-2" />
            Trigger RCA
          </Button>
        </div>

        {showForm && (
          <Card className="glass-card">
            <CardHeader>
              <CardTitle className="text-lg">Trigger Root Cause Analysis</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Agent ID</label>
                  <Input value={form.agentId} onChange={e => setForm(f => ({ ...f, agentId: e.target.value }))} placeholder="Agent GUID" />
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Anomaly Type</label>
                  <Input value={form.anomalyType} onChange={e => setForm(f => ({ ...f, anomalyType: e.target.value }))} placeholder="e.g. latency_spike, error_rate, cost_anomaly" />
                </div>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Trace ID (optional)</label>
                <Input value={form.traceId} onChange={e => setForm(f => ({ ...f, traceId: e.target.value }))} placeholder="Specific trace to analyze" />
              </div>
              <div className="flex gap-2">
                <Button onClick={handleTrigger} disabled={triggerRca.isPending || !form.agentId || !form.anomalyType}>
                  <Play className="w-4 h-4 mr-2" />
                  {triggerRca.isPending ? 'Analyzing...' : 'Run Analysis'}
                </Button>
                <Button variant="outline" onClick={() => setShowForm(false)}>Cancel</Button>
              </div>
            </CardContent>
          </Card>
        )}

        {isLoading ? (
          <div className="space-y-4">
            {[1, 2].map(i => <Skeleton key={i} className="h-40 w-full rounded-xl" />)}
          </div>
        ) : !history || history.length === 0 ? (
          <Card className="glass-card">
            <CardContent className="py-12 text-center text-muted-foreground">
              <Brain className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No root cause analyses yet</p>
              <p className="text-sm mt-1">Trigger an analysis for an anomalous agent above</p>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            {history.map(rca => <RcaCard key={rca.id} rca={rca} />)}
          </div>
        )}
      </div>
    </FadeIn>
  )
}
