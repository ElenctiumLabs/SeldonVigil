import { useState, useMemo } from 'react'
import { Shield, ShieldAlert, Search, Filter, AlertTriangle, CheckCircle2, Clock } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { DataTable, type Column } from '@/components/ui/data-table'
import { PageLoader } from '@/components/ui/page-loader'
import { useSecurityViolations, type SecurityViolation } from '@/hooks/useApi'
import { cn } from '@/lib/utils'
import { FadeIn } from '@/components/ui/animations'

// Extended interface to match API response with additional UI fields
interface Violation {
  id: string
  type: string
  severity: 'critical' | 'high' | 'medium' | 'low'
  source: string
  createdAt: string
  description: string
  status: 'open' | 'investigating' | 'resolved' | 'closed'
}

// Map API security violation to UI violation format.
// Backend value objects serialize as { value } and enums as PascalCase strings.
function mapViolation(apiViolation: SecurityViolation): Violation {
  const asId = (v: SecurityViolation['id'] | SecurityViolation['agentId']) =>
    typeof v === 'string' ? v : v?.value ?? ''

  return {
    id: asId(apiViolation.id),
    type: apiViolation.policyName || 'Policy Violation',
    severity: (apiViolation.severity || 'low').toLowerCase() as Violation['severity'],
    source: asId(apiViolation.agentId) || 'unknown',
    createdAt: apiViolation.createdAt,
    description: apiViolation.description,
    status: (apiViolation.status || 'open').toLowerCase() as Violation['status'],
  }
}

const severityConfig: Record<Violation['severity'], { label: string; variant: 'destructive' | 'warning' | 'default' | 'info'; color: string }> = {
  critical: { label: 'Critical', variant: 'destructive', color: 'text-red-600 bg-red-50 dark:bg-red-950/30' },
  high: { label: 'High', variant: 'warning', color: 'text-amber-600 bg-amber-50 dark:bg-amber-950/30' },
  medium: { label: 'Medium', variant: 'default', color: 'text-blue-600 bg-blue-50 dark:bg-blue-950/30' },
  low: { label: 'Low', variant: 'info', color: 'text-slate-600 bg-slate-50 dark:bg-slate-950/30' },
}

const statusConfig: Record<Violation['status'], { label: string; variant: 'destructive' | 'warning' | 'success' | 'secondary' }> = {
  open: { label: 'Open', variant: 'destructive' },
  investigating: { label: 'Investigating', variant: 'warning' },
  resolved: { label: 'Resolved', variant: 'success' },
  closed: { label: 'Closed', variant: 'secondary' },
}

export function SecurityPage() {
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedViolation, setSelectedViolation] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<'all' | 'critical' | 'open' | 'resolved'>('all')

  // Fetch security violations from API
  const { data: apiViolations, isLoading, error } = useSecurityViolations()

  // Map API data to UI format
  const violations = useMemo(() => {
    const violationsArray = Array.isArray(apiViolations) ? apiViolations : []
    return violationsArray.map(mapViolation)
  }, [apiViolations])

  // Filter violations based on search and tab
  const filteredViolations = useMemo(() => {
    let result = violations

    // Apply tab filter
    if (activeTab === 'critical') {
      result = result.filter((v: Violation) => v.severity === 'critical')
    } else if (activeTab === 'open') {
      result = result.filter((v: Violation) => v.status === 'open')
    } else if (activeTab === 'resolved') {
      result = result.filter((v: Violation) => v.status === 'resolved')
    }

    // Apply search filter
    if (searchTerm) {
      const term = searchTerm.toLowerCase()
      result = result.filter((v: Violation) =>
        v.id.toLowerCase().includes(term) ||
        v.type.toLowerCase().includes(term) ||
        v.source.toLowerCase().includes(term) ||
        v.description.toLowerCase().includes(term)
      )
    }

    return result
  }, [violations, searchTerm, activeTab])

  // Stats
  const criticalCount = violations.filter((v: Violation) => v.severity === 'critical').length
  const openCount = violations.filter((v: Violation) => v.status === 'open').length
  const resolvedCount = violations.filter((v: Violation) => v.status === 'resolved').length
  const highCount = violations.filter((v: Violation) => v.severity === 'high').length

  const columns: Column<Violation>[] = [
    {
      key: 'id',
      header: 'ID',
      cell: (v) => <span className="font-mono text-xs font-medium">{v.id}</span>,
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.id.localeCompare(b.id) : b.id.localeCompare(a.id),
    },
    {
      key: 'type',
      header: 'Type',
      cell: (v) => (
        <div>
          <p className="font-medium text-sm">{v.type}</p>
          <p className="text-xs text-muted-foreground mt-0.5 line-clamp-1">{v.description}</p>
        </div>
      ),
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.type.localeCompare(b.type) : b.type.localeCompare(a.type),
    },
    {
      key: 'severity',
      header: 'Severity',
      cell: (v) => (
        <Badge variant={severityConfig[v.severity].variant}>{v.severity}</Badge>
      ),
      sortable: true,
      sortFn: (a, b, dir) => {
        const order = { critical: 4, high: 3, medium: 2, low: 1 }
        return dir === 'asc' ? order[a.severity] - order[b.severity] : order[b.severity] - order[a.severity]
      },
      className: 'w-24',
    },
    {
      key: 'source',
      header: 'Source',
      cell: (v) => <span className="font-mono text-xs">{v.source}</span>,
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.source.localeCompare(b.source) : b.source.localeCompare(a.source),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (v) => (
        <Badge variant={statusConfig[v.status].variant}>{v.status}</Badge>
      ),
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc' ? a.status.localeCompare(b.status) : b.status.localeCompare(a.status),
      className: 'w-28',
    },
    {
      key: 'createdAt',
      header: 'Detected',
      cell: (v) => (
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <Clock size={12} />
          <span>{new Date(v.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</span>
        </div>
      ),
      sortable: true,
      sortFn: (a, b, dir) => dir === 'asc'
        ? new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
        : new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
      className: 'w-36',
    },
  ]

  const selectedData: Violation | null = selectedViolation ? violations.find((v: Violation) => v.id === selectedViolation) || null : null

  // Loading state
  if (isLoading) {
    return <PageLoader label="Loading security data" />
  }

  // Error state
  if (error) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Security</h1>
            <p className="page-subtitle">Monitor security violations and threats across your agent network</p>
          </div>
        </div>
        <div className="text-center py-12">
          <p className="text-destructive">Failed to load security violations</p>
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
            <h1 className="page-title font-display text-4xl gradient-text">Security</h1>
            <p className="page-subtitle">
              Monitor security violations and threats across your agent network
            </p>
          </div>
        </div>
      </FadeIn>

      {/* Stats Grid */}
      <FadeIn delay={0.1}>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {[
            { label: 'Critical Alerts', value: criticalCount, icon: ShieldAlert },
            { label: 'Open Issues', value: openCount, icon: AlertTriangle },
            { label: 'High Severity', value: highCount, icon: Shield },
            { label: 'Resolved Today', value: resolvedCount, icon: CheckCircle2 },
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

      {/* Search & Filter */}
      <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
        <div className="relative flex-1 max-w-md w-full">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search violations..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 h-10 bg-muted/50 border-0 focus-visible:bg-background rounded-xl"
          />
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" className="rounded-xl h-10">
            <Filter className="h-4 w-4 mr-2" />
            Filters
          </Button>
        </div>
      </div>

      {/* Tabs + Table */}
      <Tabs defaultValue="all" value={activeTab} onValueChange={(v) => setActiveTab(v as 'all' | 'critical' | 'open' | 'resolved')}>
        <TabsList>
          <TabsTrigger value="all">All Violations</TabsTrigger>
          <TabsTrigger value="critical">Critical</TabsTrigger>
          <TabsTrigger value="open">Open</TabsTrigger>
          <TabsTrigger value="resolved">Resolved</TabsTrigger>
        </TabsList>

        <TabsContent value="all">
          <DataTable
            data={filteredViolations}
            columns={columns}
            keyExtractor={(v) => v.id}
            pageSize={8}
            onRowClick={(v) => setSelectedViolation(v.id === selectedViolation ? null : v.id)}
            selectedKey={selectedViolation}
          />
        </TabsContent>
        <TabsContent value="critical">
          <DataTable
            data={filteredViolations}
            columns={columns}
            keyExtractor={(v) => v.id}
            pageSize={8}
            onRowClick={(v) => setSelectedViolation(v.id === selectedViolation ? null : v.id)}
            selectedKey={selectedViolation}
          />
        </TabsContent>
        <TabsContent value="open">
          <DataTable
            data={filteredViolations}
            columns={columns}
            keyExtractor={(v) => v.id}
            pageSize={8}
            onRowClick={(v) => setSelectedViolation(v.id === selectedViolation ? null : v.id)}
            selectedKey={selectedViolation}
          />
        </TabsContent>
        <TabsContent value="resolved">
          <DataTable
            data={filteredViolations}
            columns={columns}
            keyExtractor={(v) => v.id}
            pageSize={8}
            onRowClick={(v) => setSelectedViolation(v.id === selectedViolation ? null : v.id)}
            selectedKey={selectedViolation}
          />
        </TabsContent>
      </Tabs>

      {/* Detail Panel */}
      {selectedData && (
        <Card className="rounded-2xl border-border shadow-sm animate-fade-in">
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base font-semibold">{selectedData.id}: {selectedData.type}</CardTitle>
              <Badge variant={severityConfig[selectedData.severity as Violation['severity']].variant}>{selectedData.severity}</Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { label: 'Description', value: selectedData.description, fullWidth: true },
                { label: 'Source', value: selectedData.source },
                { label: 'Status', value: <Badge variant={statusConfig[selectedData.status as Violation['status']].variant}>{selectedData.status}</Badge> },
                { label: 'Detected', value: new Date(selectedData.createdAt).toLocaleString() },
                { label: 'Type', value: selectedData.type },
              ].map(detail => (
                <div key={detail.label} className={cn('p-3 rounded-xl bg-muted/30', detail.fullWidth && 'col-span-full')}>
                  <dt className="text-xs font-medium text-muted-foreground mb-1">{detail.label}</dt>
                  <dd className="text-sm font-medium text-foreground">{detail.value}</dd>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
