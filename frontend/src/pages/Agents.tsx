import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { Users, Search, Activity, Clock, PauseCircle, Info } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button, buttonVariants } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { DataTable, type Column } from '@/components/ui/data-table'
import { PageLoader } from '@/components/ui/page-loader'
import { FadeIn } from '@/components/ui/animations'
import { useToast } from '@/components/ui/toast'
import {
  useAgents,
  useActivateAgent,
  type Agent,
  type AgentStatus,
} from '@/hooks/useApi'

type StatusFilter = 'all' | 'active' | 'pending' | 'inactive'

const PENDING_STATUSES: AgentStatus[] = ['draft', 'pendingcertification']
const INACTIVE_STATUSES: AgentStatus[] = ['suspended', 'revoked', 'archived']

const statusVariant: Record<AgentStatus, 'success' | 'warning' | 'destructive' | 'secondary' | 'info'> = {
  active: 'success',
  draft: 'warning',
  pendingcertification: 'info',
  suspended: 'warning',
  revoked: 'destructive',
  archived: 'secondary',
}

function canActivate(status: AgentStatus) {
  return PENDING_STATUSES.includes(status)
}

function formatCreatedAt(value: string) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString()
}

export function AgentsPage() {
  const { data: agents, isLoading, error } = useAgents()
  const activateAgent = useActivateAgent()
  const { toast } = useToast()
  const [searchTerm, setSearchTerm] = useState('')
  const [activeTab, setActiveTab] = useState<StatusFilter>('all')
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const rows = agents ?? []

  const filtered = useMemo(() => {
    const query = searchTerm.trim().toLowerCase()
    return rows.filter(agent => {
      if (activeTab === 'active' && agent.status !== 'active') return false
      if (activeTab === 'pending' && !PENDING_STATUSES.includes(agent.status)) return false
      if (activeTab === 'inactive' && !INACTIVE_STATUSES.includes(agent.status)) return false
      if (!query) return true
      return [agent.name, agent.externalId, agent.framework, agent.modelName, agent.modelProvider]
        .join(' ')
        .toLowerCase()
        .includes(query)
    })
  }, [rows, searchTerm, activeTab])

  const selected = rows.find(agent => agent.id === selectedId) ?? null

  const stats = useMemo(() => ({
    total: rows.length,
    active: rows.filter(a => a.status === 'active').length,
    pending: rows.filter(a => PENDING_STATUSES.includes(a.status)).length,
    suspended: rows.filter(a => a.status === 'suspended').length,
  }), [rows])

  const handleActivate = async (agent: Agent) => {
    try {
      await activateAgent.mutateAsync(agent.id)
      toast({ type: 'success', title: `${agent.name} activated` })
    } catch {
      toast({
        type: 'error',
        title: 'Could not activate agent',
        message: 'The agent must be in Draft or Pending certification.',
      })
    }
  }

  const columns: Column<Agent>[] = [
    {
      key: 'name',
      header: 'Name',
      sortable: true,
      sortFn: (a, b, dir) => a.name.localeCompare(b.name) * (dir === 'asc' ? 1 : -1),
      className: 'min-w-0',
      cell: agent => (
        <div>
          <p className="font-medium text-foreground">{agent.name}</p>
          <p className="text-xs text-muted-foreground font-mono mt-0.5">
            {agent.externalId || agent.id}
          </p>
        </div>
      ),
    },
    {
      key: 'framework',
      header: 'Framework',
      sortable: true,
      sortFn: (a, b, dir) => a.framework.localeCompare(b.framework) * (dir === 'asc' ? 1 : -1),
      className: 'min-w-0',
      cell: agent => agent.framework,
    },
    {
      key: 'model',
      header: 'Model',
      className: 'min-w-0',
      cell: agent => (
        <span>
          {agent.modelName}
          {agent.modelProvider && agent.modelProvider !== 'Unknown' && (
            <span className="text-muted-foreground"> · {agent.modelProvider}</span>
          )}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      sortFn: (a, b, dir) => a.statusLabel.localeCompare(b.statusLabel) * (dir === 'asc' ? 1 : -1),
      className: 'min-w-0',
      cell: agent => (
        <Badge variant={statusVariant[agent.status] ?? 'secondary'}>{agent.statusLabel}</Badge>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      sortable: true,
      sortFn: (a, b, dir) => a.createdAt.localeCompare(b.createdAt) * (dir === 'asc' ? 1 : -1),
      className: 'min-w-0',
      cell: agent => (
        <span className="text-muted-foreground text-xs">{formatCreatedAt(agent.createdAt)}</span>
      ),
    },
    {
      key: 'actions',
      header: '',
      className: 'min-w-0',
      cell: agent => (
        <div className="flex items-center justify-end gap-2" onClick={e => e.stopPropagation()}>
          {canActivate(agent.status) && (
            <Button
              variant="outline"
              size="sm"
              disabled={activateAgent.isPending}
              onClick={() => handleActivate(agent)}
            >
              Activate
            </Button>
          )}
          <Link
            to={`/traces?agentId=${encodeURIComponent(agent.id)}`}
            className={buttonVariants({ variant: 'ghost', size: 'sm' })}
          >
            Traces
          </Link>
        </div>
      ),
    },
  ]

  if (isLoading) {
    return <PageLoader label="Loading agents" />
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Agents</h1>
            <p className="page-subtitle">Registered agents and their status</p>
          </div>
        </div>
        <div className="text-center py-12">
          <p className="text-destructive">Failed to load agents</p>
          <p className="text-sm text-muted-foreground mt-2">Check that the API is reachable and you are signed in.</p>
        </div>
      </div>
    )
  }

  const emptyState = (
    <div className="empty-state p-8 text-center">
      <Info size={48} className="mx-auto mb-4 text-muted-foreground" />
      <h3 className="font-semibold mb-2">No agents registered</h3>
      <p className="text-muted-foreground mb-4 max-w-md mx-auto">
        Inventory is empty. Register an agent, then activate it so traces can ingest.
      </p>
      <ul className="list-disc list-inside max-w-lg mx-auto text-muted-foreground text-sm space-y-2 text-left">
        <li>
          Create via CLI:
          <code className="ml-1 text-primary">seldonvigil agent create --name "Lattice"</code>
        </li>
        <li>
          Activate:
          <code className="ml-1 text-primary">seldonvigil agent activate AGENT_ID</code>
        </li>
        <li>
          Or seed demo agents and multi-span traces with one command:
          <code className="ml-1 text-emerald-400">seldonvigil seed</code>
        </li>
      </ul>
    </div>
  )

  return (
    <div className="space-y-6 overflow-x-hidden">
      <FadeIn>
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Agents</h1>
            <p className="page-subtitle">Registered agents, status, and a path into their traces</p>
          </div>
        </div>
      </FadeIn>

      <FadeIn delay={0.1}>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {[
            { label: 'Registered', value: stats.total, icon: Users },
            { label: 'Active', value: stats.active, icon: Activity },
            { label: 'Pending', value: stats.pending, icon: Clock },
            { label: 'Suspended', value: stats.suspended, icon: PauseCircle },
          ].map(stat => (
            <div key={stat.label} className="stat-card">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">{stat.label}</p>
                  <p className="text-2xl font-display font-bold mt-1">{stat.value}</p>
                </div>
                <div className="stat-icon-container flex-shrink-0">
                  <stat.icon size={22} strokeWidth={1.5} className="text-emerald-400" />
                </div>
              </div>
            </div>
          ))}
        </div>
      </FadeIn>

      <div className="relative flex-1 max-w-md w-full">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search by name, id, framework, or model..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="pl-10 h-10 bg-muted/50 border-0 focus-visible:bg-background rounded-xl"
        />
      </div>

      <Tabs defaultValue="all" value={activeTab} onValueChange={v => setActiveTab(v as StatusFilter)}>
        <TabsList>
          <TabsTrigger value="all">All</TabsTrigger>
          <TabsTrigger value="active">Active</TabsTrigger>
          <TabsTrigger value="pending">Pending</TabsTrigger>
          <TabsTrigger value="inactive">Inactive</TabsTrigger>
        </TabsList>

        {(['all', 'active', 'pending', 'inactive'] as StatusFilter[]).map(tab => (
          <TabsContent key={tab} value={tab}>
            <DataTable
              data={filtered}
              columns={columns}
              keyExtractor={agent => agent.id}
              pageSize={10}
              onRowClick={agent => setSelectedId(agent.id === selectedId ? null : agent.id)}
              selectedKey={selectedId}
              emptyState={rows.length === 0 ? emptyState : undefined}
            />
          </TabsContent>
        ))}
      </Tabs>

      {selected && (
        <Card className="rounded-2xl border-border shadow-sm animate-fade-in">
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between gap-4">
              <CardTitle className="text-base font-semibold">{selected.name}</CardTitle>
              <Badge variant={statusVariant[selected.status] ?? 'secondary'}>{selected.statusLabel}</Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { label: 'External ID', value: selected.externalId || '—' },
                { label: 'Agent ID', value: selected.id || '—' },
                { label: 'Framework', value: selected.framework },
                { label: 'Model', value: `${selected.modelName} · ${selected.modelProvider}` },
                { label: 'Created', value: formatCreatedAt(selected.createdAt) },
              ].map(detail => (
                <div key={detail.label} className="p-3 rounded-xl bg-muted/30">
                  <dt className="text-xs font-medium text-muted-foreground mb-1">{detail.label}</dt>
                  <dd className="text-sm font-medium text-foreground break-all">{detail.value}</dd>
                </div>
              ))}
            </div>
            <div className="flex gap-2 mt-4">
              {canActivate(selected.status) && (
                <Button
                  size="sm"
                  disabled={activateAgent.isPending}
                  onClick={() => handleActivate(selected)}
                >
                  Activate
                </Button>
              )}
              <Link
                to={`/traces?agentId=${encodeURIComponent(selected.id)}`}
                className={buttonVariants({ variant: 'outline', size: 'sm' })}
              >
                View traces
              </Link>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
