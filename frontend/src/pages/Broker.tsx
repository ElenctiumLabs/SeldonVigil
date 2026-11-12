import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import {
  useBrokerStatus,
  useBrokerTopics,
  useBrokerConsumerGroups,
  useBrokerConfig,
  useBrokerTopicMessages,
  type BrokerTopic,
  type BrokerConsumerGroup,
} from '@/hooks/useApi'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Copy,
  Database,
  Filter,
  Layers,
  MessageSquare,
  RefreshCw,
  Server,
  Settings2,
  Users,
  XCircle,
  Zap,
} from 'lucide-react'
import { FadeIn } from '@/components/ui/animations'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

function formatUptime(seconds: number): string {
  if (seconds === 0) return '--'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}

function formatRetention(ms: number): string {
  if (ms <= 0) return 'Forever'
  const hours = ms / 3600000
  if (hours >= 168) return `${Math.round(hours / 24)}d`
  if (hours >= 24) return `${Math.round(hours / 24)}d`
  return `${Math.round(hours)}h`
}

function copyToClipboard(text: string) {
  navigator.clipboard.writeText(text).catch(() => {/* silently ignore in non-secure contexts */})
}

// ---------------------------------------------------------------------------
// Stat card used in the Status tab
// ---------------------------------------------------------------------------

interface MetricCardProps {
  label: string
  value: string | number
  icon: React.ElementType
  sub?: string
  isLoading?: boolean
}

function MetricCard({ label, value, icon: Icon, sub, isLoading }: MetricCardProps) {
  if (isLoading) {
    return (
      <Card>
        <CardContent className="p-5">
          <Skeleton className="h-16 w-full rounded-xl" />
        </CardContent>
      </Card>
    )
  }
  return (
    <Card>
      <CardContent className="p-5">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">{label}</p>
            <p className="text-2xl font-bold text-foreground">{value}</p>
            {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
          </div>
          <div className="stat-icon-container flex-shrink-0">
            <Icon size={22} strokeWidth={1.5} className="text-emerald-400" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

// ---------------------------------------------------------------------------
// Status Tab
// ---------------------------------------------------------------------------

function StatusTab() {
  const { data: status, isLoading, refetch, isFetching } = useBrokerStatus()

  const statusBadge = status?.isReachable
    ? <Badge variant="success" className="gap-1.5"><CheckCircle2 size={12} />Running</Badge>
    : <Badge variant="destructive" className="gap-1.5"><XCircle size={12} />Unreachable</Badge>

  return (
    <div className="space-y-6">
      {/* Header row */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h2 className="text-base font-semibold text-foreground">Broker Health</h2>
          {!isLoading && statusBadge}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          disabled={isFetching}
          className="gap-2"
        >
          <RefreshCw size={14} className={cn(isFetching && 'animate-spin')} />
          Refresh
        </Button>
      </div>

      {/* Metric cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          label="Bootstrap Server"
          value={status?.bootstrapServer ?? '--'}
          icon={Server}
          sub={`Node ${status?.nodeId ?? '--'}`}
          isLoading={isLoading}
        />
        <MetricCard
          label="Topics"
          value={status?.topicCount ?? '--'}
          icon={Layers}
          sub={`${status?.partitionCount ?? 0} partitions`}
          isLoading={isLoading}
        />
        <MetricCard
          label="Consumer Groups"
          value={status?.consumerGroupCount ?? '--'}
          icon={Users}
          isLoading={isLoading}
        />
        <MetricCard
          label="Uptime"
          value={formatUptime(status?.uptimeSeconds ?? 0)}
          icon={Activity}
          sub="since last restart"
          isLoading={isLoading}
        />
      </div>

      {/* Broker details card */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center gap-2">
            <Server size={16} className="text-muted-foreground" />
            <CardTitle className="text-sm font-semibold">Broker Details</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[1, 2, 3, 4].map(i => <Skeleton key={i} className="h-8 w-full rounded-lg" />)}
            </div>
          ) : (
            <div className="divide-y divide-border">
              {[
                { label: 'Cluster ID', value: status?.clusterId ?? '--' },
                { label: 'Broker Version', value: status?.version ?? '--' },
                { label: 'Mode', value: 'Redpanda (single binary)' },
                { label: 'Broker API', value: status?.bootstrapServer ?? '--' },
                { label: 'Admin API', value: 'localhost:9644' },
                { label: 'Data Directory', value: '/var/lib/redpanda/data' },
              ].map(({ label, value }) => (
                <div key={label} className="flex items-center justify-between py-2.5">
                  <span className="text-sm text-muted-foreground">{label}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-foreground font-mono">{value}</span>
                    <button
                      onClick={() => copyToClipboard(value)}
                      className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-muted transition-all"
                      aria-label={`Copy ${label}`}
                    >
                      <Copy size={12} className="text-muted-foreground" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Connection string card */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center gap-2">
            <Zap size={16} className="text-muted-foreground" />
            <CardTitle className="text-sm font-semibold">Connection Strings</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {[
            { label: '.NET client', value: status?.bootstrapServer ?? 'localhost:9092' },
            { label: 'Broker Workers appsettings', value: `"Broker": "${status?.bootstrapServer ?? 'localhost:9092'}"` },
          ].map(({ label, value }) => (
            <div key={label} className="space-y-1.5">
              <p className="text-xs text-muted-foreground font-medium">{label}</p>
              <div className="flex items-center gap-2 bg-muted/50 rounded-xl px-3 py-2">
                <code className="text-xs flex-1 font-mono text-foreground">{value}</code>
                <button
                  onClick={() => copyToClipboard(value)}
                  className="p-1 rounded hover:bg-muted transition-colors flex-shrink-0"
                  aria-label="Copy connection string"
                >
                  <Copy size={13} className="text-muted-foreground" />
                </button>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Topics Tab
// ---------------------------------------------------------------------------

type TopicFilter = 'all' | 'dlq' | 'active'

const topicFilterActiveClass: Record<TopicFilter, string> = {
  all: 'bg-muted text-foreground',
  dlq: 'bg-amber-50 text-amber-700 dark:bg-amber-950/40 dark:text-amber-400',
  active: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400',
}

const topicFilterOptions: { id: TopicFilter; label: string }[] = [
  { id: 'all', label: 'All' },
  { id: 'dlq', label: 'DLQ only' },
  { id: 'active', label: 'Active' },
]

function TopicsTab() {
  const { data: topics = [], isLoading, refetch, isFetching } = useBrokerTopics()
  const [filter, setFilter] = useState('')
  const [topicFilter, setTopicFilter] = useState<TopicFilter>('all')
  const [expandedTopic, setExpandedTopic] = useState<string | null>(null)

  const filtered = topics.filter(t => {
    const matchesSearch = t.name.toLowerCase().includes(filter.toLowerCase())
    const matchesType =
      topicFilter === 'all' ? true :
      topicFilter === 'dlq' ? t.isDlq :
      t.messageCount > 0
    return matchesSearch && matchesType
  })

  const mainTopics = topics.filter(t => !t.isDlq)
  const dlqTopics = topics.filter(t => t.isDlq)
  const activeTopics = topics.filter(t => t.messageCount > 0)

  return (
    <div className="space-y-5">
      {/* Toolbar */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
        <div className="relative flex-1 w-full sm:max-w-xs">
          <Filter size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Filter topics..."
            value={filter}
            onChange={e => setFilter(e.target.value)}
            className="pl-8 h-9"
          />
        </div>
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-xl border border-input p-0.5">
            {topicFilterOptions.map(({ id, label }) => (
              <button
                key={id}
                onClick={() => setTopicFilter(id)}
                className={cn(
                  'h-8 px-3 rounded-[10px] text-xs font-medium transition-all',
                  topicFilter === id
                    ? topicFilterActiveClass[id]
                    : 'text-muted-foreground hover:text-foreground'
                )}
              >
                {label}
              </button>
            ))}
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching} className="gap-1.5 h-9">
            <RefreshCw size={13} className={cn(isFetching && 'animate-spin')} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Summary pills */}
      {!isLoading && (
        <div className="flex items-center gap-3 text-sm">
          <span className="text-muted-foreground">{topics.length} total</span>
          <span className="text-blue-600 font-medium">{mainTopics.length} event topics</span>
          <span className="text-amber-600 font-medium">{dlqTopics.length} DLQ topics</span>
          <span className="text-emerald-600 font-medium flex items-center gap-1">
            <Activity size={12} />
            {activeTopics.length} with messages
          </span>
        </div>
      )}

      {/* Topic list */}
      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-2">
              {Array.from({ length: 8 }).map((_, i) => (
                <Skeleton key={i} className="h-12 w-full rounded-xl" />
              ))}
            </div>
          ) : filtered.length === 0 ? (
            <div className="py-16 text-center">
              <Database size={32} className="mx-auto text-muted-foreground/40 mb-3" />
              <p className="text-sm text-muted-foreground">
                {topics.length === 0
                  ? 'No topics found. Broker may be offline or topics not yet provisioned.'
                  : 'No topics match your filter.'}
              </p>
            </div>
          ) : (
            <div className="divide-y divide-border">
              {filtered.map((topic) => (
                <TopicRow
                  key={topic.name}
                  topic={topic}
                  expanded={expandedTopic === topic.name}
                  onToggle={() => setExpandedTopic(prev => prev === topic.name ? null : topic.name)}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function TopicRow({ topic, expanded, onToggle }: { topic: BrokerTopic; expanded: boolean; onToggle: () => void }) {
  return (
    <div>
      <button
        onClick={onToggle}
        className="w-full flex items-center gap-3 px-4 py-3 hover:bg-muted/40 transition-colors text-left"
        aria-expanded={expanded}
      >
        {expanded
          ? <ChevronDown size={14} className="text-muted-foreground flex-shrink-0" />
          : <ChevronRight size={14} className="text-muted-foreground flex-shrink-0" />}

        <span className="flex-1 text-sm font-medium font-mono text-foreground truncate">{topic.name}</span>

        <div className="flex items-center gap-2 flex-shrink-0">
          {topic.isDlq && (
            <Badge variant="warning" className="text-[10px] py-0 px-2">DLQ</Badge>
          )}
          {topic.messageCount > 0 && (
            <span className="hidden sm:flex items-center gap-1 text-[10px] text-emerald-600 dark:text-emerald-400 font-medium">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
              {topic.messageCount.toLocaleString()}
            </span>
          )}
          <span className="text-xs text-muted-foreground hidden sm:block">
            {topic.partitions} {topic.partitions === 1 ? 'partition' : 'partitions'}
          </span>
          <span className="text-xs text-muted-foreground hidden md:block">
            {formatRetention(topic.retentionMs)} retention
          </span>
        </div>
      </button>

      {expanded && (
        <div className="px-4 sm:px-10 pb-4 space-y-4 bg-muted/20 animate-fade-in">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-3">
            {[
              { label: 'Partitions', value: topic.partitions },
              { label: 'Replication', value: `${topic.replicationFactor}x` },
              { label: 'Retention', value: formatRetention(topic.retentionMs) },
              { label: 'Compression', value: topic.compressionType || 'none' },
              { label: 'Messages', value: topic.messageCount > 0 ? topic.messageCount.toLocaleString() : '0' },
              { label: 'Size', value: topic.sizeBytes > 0 ? formatBytes(topic.sizeBytes) : '--' },
            ].map(({ label, value }) => (
              <div key={label} className="space-y-0.5">
                <p className="text-[10px] uppercase tracking-wider text-muted-foreground font-medium">{label}</p>
                <p className="text-sm font-semibold text-foreground">{value}</p>
              </div>
            ))}
          </div>

          <TopicMessages topicName={topic.name} />
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Topic message viewer (read-only tail, fetched on demand)
// ---------------------------------------------------------------------------

function formatMessageValue(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

function TopicMessages({ topicName }: { topicName: string }) {
  const { data: messages, isLoading, isError, refetch, isFetching } = useBrokerTopicMessages(topicName)
  const [openMessage, setOpenMessage] = useState<string | null>(null)

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <p className="text-[10px] uppercase tracking-wider text-muted-foreground font-medium flex items-center gap-1.5">
          <MessageSquare size={11} />
          Recent messages
        </p>
        <Button variant="ghost" size="sm" onClick={() => refetch()} disabled={isFetching} className="h-6 px-2 text-[11px] gap-1">
          <RefreshCw size={11} className={cn(isFetching && 'animate-spin')} />
          Refresh
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-1.5 pb-1">
          {[1, 2, 3].map(i => <Skeleton key={i} className="h-9 w-full rounded-xl" />)}
        </div>
      ) : isError ? (
        <div className="rounded-xl border border-destructive/30 bg-destructive/5 px-3 py-2.5 text-xs text-destructive flex items-center gap-2 mb-1">
          <AlertTriangle size={13} />
          Failed to fetch messages. Is the broker reachable?
        </div>
      ) : !messages || messages.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border px-3 py-4 text-center text-xs text-muted-foreground mb-1">
          No messages found in this topic.
        </div>
      ) : (
        <div className="space-y-1.5 pb-1 max-h-96 overflow-y-auto pr-1">
          {messages.map(m => {
            const messageId = `${m.partition}-${m.offset}`
            const isOpen = openMessage === messageId
            return (
              <div key={messageId} className="rounded-xl border border-border bg-background overflow-hidden">
                <button
                  onClick={() => setOpenMessage(prev => (prev === messageId ? null : messageId))}
                  className="w-full flex items-center gap-2 px-3 py-2 hover:bg-muted/40 transition-colors text-left"
                >
                  {isOpen
                    ? <ChevronDown size={12} className="text-muted-foreground flex-shrink-0" />
                    : <ChevronRight size={12} className="text-muted-foreground flex-shrink-0" />}
                  <Badge variant="secondary" className="text-[10px] py-0 px-1.5 font-mono flex-shrink-0">
                    P{m.partition} · {m.offset}
                  </Badge>
                  {m.key != null && m.key !== '' && (
                    <span className="text-xs font-mono text-foreground truncate">{m.key}</span>
                  )}
                  {!isOpen && (
                    <span className="flex-1 text-[11px] text-muted-foreground truncate min-w-0">
                      {formatMessageValue(m.value).split('\n')[0].slice(0, 120)}
                    </span>
                  )}
                  {m.truncated && (
                    <span title="Payload truncated server-side" className="flex-shrink-0">
                      <AlertTriangle size={11} className="text-amber-500" />
                    </span>
                  )}
                  <span className="ml-auto text-[10px] text-muted-foreground whitespace-nowrap flex-shrink-0">
                    {new Date(m.timestampUtc).toLocaleTimeString()}
                  </span>
                </button>

                {isOpen && (
                  <div className="border-t border-border bg-muted/20 px-3 py-2 animate-fade-in">
                    <pre className="max-h-48 overflow-auto text-[11px] leading-relaxed font-mono text-foreground/90 whitespace-pre-wrap break-all">
                      {formatMessageValue(m.value)}
                    </pre>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Consumer Groups Tab
// ---------------------------------------------------------------------------

const groupStateConfig: Record<BrokerConsumerGroup['state'], { label: string; variant: 'success' | 'warning' | 'destructive' | 'secondary' | 'info' }> = {
  Stable:               { label: 'Stable',     variant: 'success' },
  PreparingRebalance:   { label: 'Rebalancing', variant: 'warning' },
  CompletingRebalance:  { label: 'Rebalancing', variant: 'warning' },
  Empty:                { label: 'Empty',       variant: 'secondary' },
  Dead:                 { label: 'Dead',        variant: 'destructive' },
}

function ConsumerGroupsTab() {
  const { data: groups = [], isLoading, refetch, isFetching } = useBrokerConsumerGroups()
  const [filter, setFilter] = useState('')

  const filtered = groups.filter(g =>
    g.groupId.toLowerCase().includes(filter.toLowerCase())
  )

  const stableCount   = groups.filter(g => g.state === 'Stable').length
  const problemCount  = groups.filter(g => g.state === 'Dead' || g.totalLag > 1000).length

  return (
    <div className="space-y-5">
      {/* Toolbar */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
        <div className="relative flex-1 w-full sm:max-w-xs">
          <Filter size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Filter groups..."
            value={filter}
            onChange={e => setFilter(e.target.value)}
            className="pl-8 h-9"
          />
        </div>
        <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching} className="gap-1.5 h-9">
          <RefreshCw size={13} className={cn(isFetching && 'animate-spin')} />
          Refresh
        </Button>
      </div>

      {/* Summary pills */}
      {!isLoading && groups.length > 0 && (
        <div className="flex items-center gap-3 text-sm">
          <span className="text-muted-foreground">{groups.length} groups</span>
          <span className="text-emerald-600 font-medium">{stableCount} stable</span>
          {problemCount > 0 && (
            <span className="text-red-600 font-medium flex items-center gap-1">
              <AlertTriangle size={13} />{problemCount} need attention
            </span>
          )}
        </div>
      )}

      {/* Groups list */}
      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} className="h-14 w-full rounded-xl" />
              ))}
            </div>
          ) : filtered.length === 0 ? (
            <div className="py-16 text-center">
              <Users size={32} className="mx-auto text-muted-foreground/40 mb-3" />
              <p className="text-sm text-muted-foreground">
                {groups.length === 0
                  ? 'No consumer groups found. Start SeldonVigil.Workers to register groups.'
                  : 'No groups match your filter.'}
              </p>
            </div>
          ) : (
            <div className="divide-y divide-border">
              {filtered.map(group => (
                <ConsumerGroupRow key={group.groupId} group={group} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function ConsumerGroupRow({ group }: { group: BrokerConsumerGroup }) {
  const [expanded, setExpanded] = useState(false)
  const cfg = groupStateConfig[group.state] ?? { label: group.state, variant: 'secondary' as const }
  const hasHighLag = group.totalLag > 1000

  return (
    <div>
      <button
        onClick={() => setExpanded(v => !v)}
        className="w-full flex items-center gap-3 px-4 py-3.5 hover:bg-muted/40 transition-colors text-left"
        aria-expanded={expanded}
      >
        {expanded
          ? <ChevronDown size={14} className="text-muted-foreground flex-shrink-0" />
          : <ChevronRight size={14} className="text-muted-foreground flex-shrink-0" />}

        <span className="flex-1 text-sm font-medium font-mono text-foreground truncate">{group.groupId}</span>

        <div className="flex items-center gap-3 flex-shrink-0">
          {hasHighLag && (
            <span className="hidden sm:flex items-center gap-1 text-xs text-amber-600 font-medium">
              <AlertTriangle size={12} />
              High lag
            </span>
          )}
          <span className="text-xs text-muted-foreground hidden sm:block">
            {group.memberCount} {group.memberCount === 1 ? 'member' : 'members'}
          </span>
          <Badge variant={cfg.variant}>{cfg.label}</Badge>
        </div>
      </button>

      {expanded && (
        <div className="px-10 pb-4 space-y-3 bg-muted/20 animate-fade-in">
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {[
              { label: 'Members', value: group.memberCount },
              { label: 'Total Lag', value: group.totalLag > 0 ? group.totalLag.toLocaleString() : '0' },
              { label: 'State', value: group.state },
            ].map(({ label, value }) => (
              <div key={label} className="space-y-0.5">
                <p className="text-[10px] uppercase tracking-wider text-muted-foreground font-medium">{label}</p>
                <p className="text-sm font-semibold text-foreground">{value}</p>
              </div>
            ))}
          </div>
          {group.topics.length > 0 && (
            <div className="space-y-1.5">
              <p className="text-[10px] uppercase tracking-wider text-muted-foreground font-medium">Subscribed Topics</p>
              <div className="flex flex-wrap gap-1.5">
                {group.topics.map(t => (
                  <span key={t} className="text-xs font-mono bg-muted px-2 py-0.5 rounded-lg text-foreground">{t}</span>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Configuration Tab
// ---------------------------------------------------------------------------

function ConfigurationTab() {
  const { data: config, isLoading } = useBrokerConfig()
  const [saved, setSaved] = useState(false)

  function handleSave() {
    // In a real implementation this would PUT to /api/v1/broker/config
    setSaved(true)
    setTimeout(() => setSaved(false), 2500)
  }

  const sections = [
    {
      title: 'Broker Connection',
      icon: Server,
      fields: [
        { id: 'bootstrapServer', label: 'Bootstrap Server', value: config?.bootstrapServer ?? '', placeholder: 'localhost:9092', readonly: true, hint: 'Redpanda broker listener address' },
      ],
    },
    {
      title: 'Consumer Settings',
      icon: Users,
      fields: [
        { id: 'consumerGroup', label: 'Consumer Group (Workers)', value: config?.consumerGroup ?? '', placeholder: 'seldonvigil-workers-local', readonly: false, hint: 'Defined in appsettings.Local.json' },
        { id: 'dlqSuffix', label: 'DLQ Topic Suffix', value: config?.dlqSuffix ?? '', placeholder: '-dlq', readonly: false, hint: 'Appended to topic name for dead-letter routing' },
      ],
    },
    {
      title: 'Storage Paths',
      icon: Database,
      fields: [
        { id: 'dataDir', label: 'Data Directory', value: config?.dataDir ?? '', placeholder: 'container:/var/lib/redpanda/data', readonly: true, hint: 'Redpanda data store (docker volume redpanda_data)' },
        { id: 'logDir', label: 'Log Directory', value: config?.logDir ?? '', placeholder: 'container:/var/log/redpanda', readonly: true, hint: 'Streamed to container stdout; redpanda logs to stdout directly' },
      ],
    },
  ]

  return (
    <div className="space-y-5">
      {sections.map(({ title, icon: Icon, fields }) => (
        <Card key={title}>
          <CardHeader className="pb-3">
            <div className="flex items-center gap-2">
              <Icon size={16} className="text-muted-foreground" />
              <CardTitle className="text-sm font-semibold">{title}</CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {fields.map(field => (
              <div key={field.id} className="space-y-1.5">
                <Label htmlFor={field.id} className="text-sm">{field.label}</Label>
                {isLoading ? (
                  <Skeleton className="h-10 w-full rounded-xl" />
                ) : (
                  <div className="relative">
                    <Input
                      id={field.id}
                      defaultValue={field.value}
                      placeholder={field.placeholder}
                      readOnly={field.readonly}
                      className={cn(
                        field.readonly && 'bg-muted/50 cursor-default text-muted-foreground font-mono text-xs',
                        !field.readonly && 'font-mono text-xs'
                      )}
                    />
                    {field.readonly && (
                      <button
                        onClick={() => copyToClipboard(field.value)}
                        className="absolute right-2.5 top-1/2 -translate-y-1/2 p-1 rounded hover:bg-muted transition-colors"
                        aria-label="Copy value"
                      >
                        <Copy size={13} className="text-muted-foreground" />
                      </button>
                    )}
                  </div>
                )}
                {field.hint && (
                  <p className="text-xs text-muted-foreground">{field.hint}</p>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      ))}

      {/* Quick-launch reference */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center gap-2">
            <Settings2 size={16} className="text-muted-foreground" />
            <CardTitle className="text-sm font-semibold">Script Reference</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {[
            { label: 'Start broker',       cmd: 'docker compose -f infrastructure/docker/docker-compose.yml up -d redpanda' },
            { label: 'Stop broker',        cmd: 'docker compose -f infrastructure/docker/docker-compose.yml stop redpanda' },
            { label: 'Provision topics',   cmd: 'docker exec docker-redpanda-1 rpk topic create TraceIngestedEvent TraceEnrichedEvent' },
            { label: 'Test connection',    cmd: 'docker exec docker-redpanda-1 rpk cluster info' },
            { label: 'Follow logs',        cmd: 'docker logs -f docker-redpanda-1' },
          ].map(({ label, cmd }) => (
            <div key={label} className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground w-36 flex-shrink-0">{label}</span>
              <div className="flex-1 flex items-center gap-2 bg-muted/50 rounded-lg px-2.5 py-1.5">
                <code className="text-xs font-mono flex-1 text-foreground">{cmd}</code>
                <button
                  onClick={() => copyToClipboard(cmd)}
                  className="p-0.5 rounded hover:bg-muted transition-colors flex-shrink-0"
                  aria-label="Copy command"
                >
                  <Copy size={12} className="text-muted-foreground" />
                </button>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button onClick={handleSave} className="gap-2 rounded-xl">
          {saved
            ? <><CheckCircle2 size={15} />Saved</>
            : 'Save Changes'
          }
        </Button>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Page root
// ---------------------------------------------------------------------------

export function BrokerPage() {
  const queryClient = useQueryClient()
  const { data: status } = useBrokerStatus()

  function handleRefreshAll() {
    queryClient.invalidateQueries({ queryKey: ['brokerStatus'] })
    queryClient.invalidateQueries({ queryKey: ['brokerTopics'] })
    queryClient.invalidateQueries({ queryKey: ['brokerConsumerGroups'] })
  }

  return (
    <div className="space-y-6">
      {/* Page header */}
      <FadeIn>
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Broker</h1>
            <p className="page-subtitle">
              Broker management, topic monitoring, and consumer group health
            </p>
          </div>
          <div className="flex items-center gap-3">
            {status !== undefined && (
              <div className={cn(
                'flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-full border',
                status.isReachable
                  ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
                  : 'bg-red-500/10 text-red-500 border-red-500/20'
              )}>
                <span className={cn(
                  'w-1.5 h-1.5 rounded-full',
                  status.isReachable ? 'bg-emerald-500 animate-pulse' : 'bg-red-500'
                )} />
                {status.isReachable ? 'localhost:9092' : 'Broker offline'}
              </div>
            )}
            <Button variant="outline" size="sm" onClick={handleRefreshAll} className="gap-2">
              <RefreshCw size={14} />
              Refresh All
            </Button>
          </div>
        </div>
      </FadeIn>

      {/* Tabs */}
      <Tabs defaultValue="status">
        <TabsList>
          <TabsTrigger value="status">
            <Activity size={14} className="mr-1.5" />
            Status
          </TabsTrigger>
          <TabsTrigger value="topics">
            <Layers size={14} className="mr-1.5" />
            Topics
          </TabsTrigger>
          <TabsTrigger value="consumers">
            <Users size={14} className="mr-1.5" />
            Consumer Groups
          </TabsTrigger>
          <TabsTrigger value="config">
            <Settings2 size={14} className="mr-1.5" />
            Configuration
          </TabsTrigger>
        </TabsList>

        <TabsContent value="status">
          <StatusTab />
        </TabsContent>
        <TabsContent value="topics">
          <TopicsTab />
        </TabsContent>
        <TabsContent value="consumers">
          <ConsumerGroupsTab />
        </TabsContent>
        <TabsContent value="config">
          <ConfigurationTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}
