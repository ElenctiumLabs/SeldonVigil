import axios from 'axios'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

// Relative URL — goes through the Vite dev proxy to https://localhost:61993.
// Never hardcode the HTTPS backend URL here; the browser blocks self-signed
// certs and CORS will prevent direct cross-origin requests.
const API_BASE = '/api/v1'

export interface TraceEventItem {
  name: string
  value: string
}

export interface TraceDto {
  id: string
  agentId: string
  status: string
  createdAt: string
  durationMs: number
  error?: string
  costUsd: number
  operationName?: string
  operationType?: string
  modelName?: string
  parentTraceId?: string | null
  totalTokens?: number
  costUsdTotal?: number
  agentName?: string
  statusMessage?: string
  attributes?: Record<string, string>
  events?: TraceEventItem[]
  children?: TraceDto[]
}

const apiClient = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor to attach auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor - individual hooks handle error/loading states via React Query
apiClient.interceptors.response.use(
  (response) => response,
  (error) => Promise.reject(error)
)

// Cost Trend Hook - maps to /api/v1/costs/dashboard
interface CostTrendParams {
  agentId?: string
  days?: number
}

interface CostTrendData {
  amount: any
  tokens: number
  date: string
  cost: number
  budget?: number
}

interface CostDashboardDto {
  totalSpend: { amount: number; currency: string }
  budgetAmount: { amount: number; currency: string }
  budgetUtilizationPercent: number
  breakdownByProvider: CostBreakdownDto[]
  breakdownByModel: CostBreakdownDto[]
  breakdownByAgent: CostBreakdownDto[]
  dailyTrend: CostTrendData[]
}

interface CostBreakdownDto {
  category: string
  amount: { amount: number; currency: string }
  percentageOfTotal: number
  tokens: number
  calls: number
}

// ── Cost Budgets ─────────────────────────────────────────────────────────

export interface CreateBudgetParams {
  name: string
  scopeType: 'Tenant' | 'Team' | 'Project' | 'Agent'
  scopeId: string
  amount: number
  currency?: string
  period?: 'Daily' | 'Weekly' | 'Monthly' | 'Quarterly' | 'Annual'
  alertThresholds?: number[]
  enforcementType?: 'Soft' | 'Hard'
}

export interface CostBudgetItem {
  id: { value: string }
  name: string
  scopeType: string
  scopeId: string
  budgetAmount: { amount: number; currency: string }
  currentSpend: { amount: number; currency: string }
  utilizationPercent: number
  status: string
}

export async function createBudget(params: CreateBudgetParams): Promise<void> {
  await apiClient.post('/costs/budgets', {
    Name: params.name,
    ScopeType: params.scopeType,
    ScopeId: params.scopeId,
    Amount: params.amount,
    Currency: params.currency ?? 'USD',
    Period: params.period ?? 'Monthly',
    AlertThresholds: params.alertThresholds ?? [0.8, 1.0],
    EnforcementType: params.enforcementType ?? 'Soft',
  })

}
export function useBudgets() {
  return useQuery({
    queryKey: ['costBudgets'],
    queryFn: async () => {
      const response = await apiClient.get<CostBudgetItem[]>('/costs/budgets')
      return response.data
    },
    refetchInterval: 30000,
  })
}
export function useCostTrend({ agentId, days = 7 }: CostTrendParams) {
  return useQuery({
    queryKey: ['costTrend', agentId, days],
    queryFn: async () => {
      const response = await apiClient.get<CostDashboardDto>('/costs/dashboard')
      return response.data.dailyTrend || []
    },
  })
}

// Traces Hook
export interface TraceParams {
  agentId?: string
  status?: string
  from?: string
  to?: string
  limit?: number
  offset?: number
}

export interface Trace {
  id: string
  agentId: string
  operationName: string
  operationType: string
  modelName: string
  parentTraceId: string | null
  tokenTotal: number
  costUsd: number
  startedAt: string
  durationMs: number
  status: string
  agentName: string
  statusMessage?: string
  attributes?: Record<string, string>
  events?: TraceEventItem[]
  children?: Trace[]
}

export function mapTraceDto(dto: TraceDto): Trace {
  const rawStatus = (dto.status || '').toLowerCase()
  const status = rawStatus === 'ok' || rawStatus === 'success' ? 'success' : (rawStatus || 'unknown')

  return {
    id: dto.id,
    agentId: dto.agentId,
    operationName: dto.operationName || dto.error || dto.id,
    operationType: dto.operationType || 'custom',
    modelName: dto.modelName || 'unknown',
    parentTraceId: dto.parentTraceId ?? null,
    tokenTotal: dto.totalTokens ?? 0,
    costUsd: dto.costUsd ?? dto.costUsdTotal ?? 0,
    startedAt: dto.createdAt,
    durationMs: dto.durationMs ?? 0,
    status,
    agentName: dto.agentName || dto.agentId,
    statusMessage: dto.statusMessage ?? dto.error,
    attributes: dto.attributes,
    events: dto.events,
    children: dto.children?.map(mapTraceDto) || [],
  }
}

export function useTraces(params?: TraceParams) {
  return useQuery({
    queryKey: ['traces', params],
    queryFn: async () => {
      const response = await apiClient.get<TraceDto[]>('/observability/traces', { params })
      return (response.data || []).map(mapTraceDto)
    },
    refetchInterval: 30000,
  })
}

// Trace Details Hook
export function useTraceDetails(traceId: string) {
  return useQuery({
    queryKey: ['traceDetails', traceId],
    queryFn: async () => {
      const response = await apiClient.get<TraceDto>(`/observability/traces/${traceId}`)
      return mapTraceDto(response.data)
    },
    enabled: !!traceId,
  })
}

// Security Violations Hook - maps to /api/v1/security/violations
export interface SecurityViolation {
  id: { value: string } | string
  policyId?: { value: string }
  policyName?: string
  agentId?: { value: string } | string
  traceId?: { value: string }
  severity: 'low' | 'medium' | 'high' | 'critical'
  createdAt: string
  description: string
  status?: string
}

export function useSecurityViolations(params?: { from?: string; to?: string; severity?: string; status?: string }) {
  return useQuery({
    queryKey: ['securityViolations', params],
    queryFn: async () => {
      const response = await apiClient.get<SecurityViolation[]>('/security/violations', { params })
      return response.data
    },
  })
}

// Health Status Hook - uses correct backend path (/healthz, not /api/v1/healthz)
// Backend returns: { status: "Healthy", checks: [{ name, status, description, duration }] }
interface HealthCheckEntry {
  name: string
  status: string
  description?: string
  duration: number
}

interface HealthReport {
  status: string
  checks: HealthCheckEntry[]
}

// Derived status for the frontend display
interface HealthStatus {
  status: 'healthy' | 'degraded' | 'unhealthy'
  services: {
    name: string
    status: 'healthy' | 'unhealthy'
  }[]
}

export function useHealthStatus() {
  return useQuery({
    queryKey: ['healthStatus'],
    queryFn: async () => {
      // Health endpoint is at /healthz (not under /api/v1)
      const response = await axios.get<HealthReport>('/healthz')
      const report = response.data

      // Map backend response to frontend interface
      return {
        status: report.status.toLowerCase() as HealthStatus['status'],
        services: report.checks.map(check => ({
          name: check.name,
          status: check.status.toLowerCase() as 'healthy' | 'unhealthy',
        })),
      } satisfies HealthStatus
    },
    refetchInterval: 30000,
  })
}

// Agent Costs Hook - maps to /api/v1/costs/dashboard
interface CostDashboardDto {
  totalSpend: { amount: number; currency: string }
  budgetAmount: { amount: number; currency: string }
  budgetUtilizationPercent: number
  breakdownByProvider: CostBreakdownDto[]
  breakdownByModel: CostBreakdownDto[]
  breakdownByAgent: CostBreakdownDto[]
  dailyTrend: CostTrendData[]
}

interface CostBreakdownDto {
  category: string
  amount: { amount: number; currency: string }
  percentageOfTotal: number
  tokens: number
  calls: number
}

export interface AgentCostApi {
  agent: string
  cost: number
  tokens: number
  calls: number
  avgLatency: number
  trend: 'up' | 'down'
}

export function useAgentCosts() {
  return useQuery({
    queryKey: ['agentCosts'],
    queryFn: async () => {
      const response = await apiClient.get<CostDashboardDto>('/costs/dashboard')
      const breakdown = response.data.breakdownByAgent || []

      return breakdown.map((item) => ({
        agent: item.category,
        cost: item.amount.amount,
        tokens: item.tokens ?? 0,
        calls: item.calls ?? 0,
        avgLatency: item.calls ? Math.round((item.amount.amount / item.calls) * 10000) / 10000 : 0,
        trend: (item.percentageOfTotal > 50 ? 'up' : 'down') as 'up' | 'down',
      })) as AgentCostApi[]
    },
  })
}

// Agents List Hook - maps to GET /api/v1/agents (AgentListItemDto)
type WireId = string | { value: string }

export type AgentStatus =
  | 'draft'
  | 'pendingcertification'
  | 'active'
  | 'suspended'
  | 'revoked'
  | 'archived'

export interface Agent {
  id: string
  externalId: string
  name: string
  framework: string
  modelProvider: string
  modelName: string
  status: AgentStatus
  statusLabel: string
  createdAt: string
}

interface AgentListItemDto {
  id: WireId
  externalId?: string
  name?: string
  framework?: string
  modelProvider?: string
  modelName?: string
  status?: string
  createdAt?: string
}

interface PaginatedAgents {
  items?: AgentListItemDto[]
  totalCount?: number
}

function unwrapWireId(id: WireId | undefined): string {
  if (!id) return ''
  if (typeof id === 'string') return id
  return id.value ?? ''
}

function formatStatusLabel(status: string) {
  return status
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/^./, c => c.toUpperCase())
}

function mapAgent(item: AgentListItemDto): Agent {
  const rawStatus = item.status || 'Draft'
  return {
    id: unwrapWireId(item.id),
    externalId: item.externalId || '',
    name: item.name || 'Unnamed agent',
    framework: item.framework || 'Unknown',
    modelProvider: item.modelProvider || 'Unknown',
    modelName: item.modelName || 'Unknown',
    status: rawStatus.replace(/\s+/g, '').toLowerCase() as AgentStatus,
    statusLabel: formatStatusLabel(rawStatus),
    createdAt: item.createdAt || '',
  }
}

export function useAgents() {
  return useQuery({
    queryKey: ['agents'],
    queryFn: async () => {
      const response = await apiClient.get<PaginatedAgents>('/agents', {
        params: { page: 1, pageSize: 100 },
      })
      return (response.data.items ?? []).map(mapAgent)
    },
  })
}

export function useActivateAgent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/agents/${id}/activate`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] })
    },
  })
}

// Notifications Hook
type NotificationType = 'security_alert' | 'cost_anomaly' | 'policy_violation' | 'system'

interface Notification {
  id: string
  type: NotificationType
  title: string
  message: string
  agentId?: string
  severity: 'info' | 'warning' | 'critical'
  createdAt: string
  read: boolean
}

export function useNotifications() {
  return useQuery({
    queryKey: ['notifications'],
    queryFn: async () => {
      const response = await apiClient.get<Notification[]>('/notifications')
      return response.data
    },
    refetchInterval: 60000,
  })
}

// Settings Hook
interface AppSettings {
  theme: 'light' | 'dark' | 'system'
  notifications: {
    email: boolean
    slack: boolean
    securityAlerts: boolean
    costAlerts: boolean
  }
  api: {
    pollingInterval: number
    timeout: number
  }
  budget: {
    monthlyLimit: number
    alertsEnabled: boolean
  }
}

export function useSettings() {
  return useQuery({
    queryKey: ['settings'],
    queryFn: async () => {
      const response = await apiClient.get<AppSettings>('/settings')
      return response.data
    },
  })
}

export default apiClient

// ---------------------------------------------------------------------------
// Broker Management Hooks
// ---------------------------------------------------------------------------

export interface BrokerStatus {
  bootstrapServer: string
  nodeId: number
  clusterId: string
  isReachable: boolean
  topicCount: number
  partitionCount: number
  consumerGroupCount: number
  version: string
  uptimeSeconds: number
}

export interface BrokerTopic {
  name: string
  partitions: number
  replicationFactor: number
  retentionMs: number
  compressionType: string
  messageCount: number
  sizeBytes: number
  isDlq: boolean
}

export interface BrokerConsumerGroup {
  groupId: string
  state: 'Stable' | 'PreparingRebalance' | 'CompletingRebalance' | 'Empty' | 'Dead'
  memberCount: number
  topics: string[]
  totalLag: number
}

export interface BrokerConfig {
  bootstrapServer: string
  consumerGroup: string
  dlqSuffix: string
  heapOpts: string
  logDir: string
  dataDir: string
}

export interface BrokerTopicMessage {
  partition: number
  offset: number
  key: string | null
  value: string
  timestampUtc: string
  headers?: Record<string, string> | null
  truncated?: boolean
}

// Broker status — polls every 15s
export function useBrokerStatus() {
  return useQuery({
    queryKey: ['brokerStatus'],
    queryFn: async (): Promise<BrokerStatus> => {
      try {
        const response = await apiClient.get<BrokerStatus>('/broker/status')
        return response.data
      } catch {
        // Return a "not reachable" seldonvigil when the backend can't reach the broker
        return {
          bootstrapServer: 'localhost:9092',
          nodeId: 1,
          clusterId: 'unreachable',
          isReachable: false,
          topicCount: 0,
          partitionCount: 0,
          consumerGroupCount: 0,
          version: 'Redpanda',
          uptimeSeconds: 0,
        }
      }
    },
    refetchInterval: 15000,
  })
}

// Topic list
export function useBrokerTopics() {
  return useQuery({
    queryKey: ['brokerTopics'],
    queryFn: async (): Promise<BrokerTopic[]> => {
      try {
        const response = await apiClient.get<BrokerTopic[]>('/broker/topics')
        return response.data
      } catch {
        return []
      }
    },
    refetchInterval: 30000,
  })
}

// Consumer groups
export function useBrokerConsumerGroups() {
  return useQuery({
    queryKey: ['brokerConsumerGroups'],
    queryFn: async (): Promise<BrokerConsumerGroup[]> => {
      try {
        const response = await apiClient.get<BrokerConsumerGroup[]>('/broker/consumer-groups')
        return response.data
      } catch {
        return []
      }
    },
    refetchInterval: 15000,
  })
}

// Most recent messages of a topic — only fetched on demand (enabled), never polled
export function useBrokerTopicMessages(topicName: string | null) {
  return useQuery({
    queryKey: ['brokerTopicMessages', topicName],
    enabled: !!topicName,
    staleTime: 5000,
    retry: false,
    queryFn: async (): Promise<BrokerTopicMessage[]> => {
      const response = await apiClient.get<BrokerTopicMessage[]>(
        `/broker/topics/${encodeURIComponent(topicName!)}/messages`,
        { params: { limit: 20 } }
      )
      return response.data
    },
  })
}

// Static config (read from appsettings, not live broker)
export function useBrokerConfig() {
  return useQuery({
    queryKey: ['brokerConfig'],
    queryFn: async (): Promise<BrokerConfig> => {
      try {
        const response = await apiClient.get<BrokerConfig>('/broker/config')
        return response.data
      } catch {
        return {
          bootstrapServer: 'localhost:9092',
          consumerGroup: 'seldonvigil-workers-local',
          dlqSuffix: '.dlq',
          heapOpts: 'none (no JVM)',
          logDir: 'container:/var/log/redpanda',
          dataDir: 'container:/var/lib/redpanda/data',
        }
      }
    },
  })
}

// ==================== Governance ====================

export interface GovernancePolicy {
  id: string
  tenantId: string
  name: string
  description: string
  category: string
  definitionYaml: string
  status: string
  version: number
  previousVersionId: string | null
  createdAt: string
  activatedAt: string | null
  deprecatedAt: string | null
}

export interface CertificationPipeline {
  id: string
  tenantId: string
  agentId: string
  status: string
  currentStage: string | null
  stages: PipelineStage[]
  startedAt: string
  completedAt: string | null
  approvalNotes: string | null
}

export interface PipelineStage {
  name: string
  description: string
  isRequired: boolean
  status: string
  startedAt: string | null
  completedAt: string | null
  output: string | null
}

export function useGovernancePolicies() {
  return useQuery({
    queryKey: ['governancePolicies'],
    queryFn: async () => {
      const response = await apiClient.get<GovernancePolicy[]>('/governance/policies')
      return response.data
    },
  })
}

export function useCertificationPipelines(agentId?: string) {
  return useQuery({
    queryKey: ['certificationPipelines', agentId],
    queryFn: async () => {
      // Without an agent, list the tenant's pipelines; with one, list that agent's.
      const path = agentId
        ? `/governance/certification/agent/${agentId}`
        : '/governance/certification'
      const response = await apiClient.get<CertificationPipeline[]>(path)
      return response.data
    },
    enabled: true,
  })
}

export function useTriggerCertification() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (agentId: string) => {
      const response = await apiClient.post<CertificationPipeline>('/governance/certification/trigger', { agentId })
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['certificationPipelines'] })
    },
  })
}

export function useApproveCertification() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, notes }: { id: string; notes?: string }) => {
      const response = await apiClient.post<CertificationPipeline>(`/governance/certification/${id}/approve`, { notes })
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['certificationPipelines'] })
    },
  })
}

export function useCreatePolicy() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (policy: { name: string; description: string; category: string; definitionYaml: string }) => {
      const response = await apiClient.post<GovernancePolicy>('/governance/policies', policy)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['governancePolicies'] })
    },
  })
}

export function useActivatePolicy() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<GovernancePolicy>(`/governance/policies/${id}/activate`)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['governancePolicies'] })
    },
  })
}

// ==================== Integrations ====================

export interface Integration {
  id: string
  tenantId: string
  name: string
  type: string
  status: string
  createdAt: string
  lastUsedAt: string | null
  errorCount: number
}

export function useIntegrations() {
  return useQuery({
    queryKey: ['integrations'],
    queryFn: async () => {
      const response = await apiClient.get<Integration[]>('/integrations')
      return response.data
    },
  })
}

export function useCreateIntegration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (integration: { name: string; type: string; configurationJson: string; webhookUrl?: string; apiKey?: string }) => {
      const response = await apiClient.post<Integration>('/integrations', integration)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['integrations'] })
    },
  })
}

export function useTestIntegration() {
  return useMutation({
    mutationFn: async ({ id, message }: { id: string; message?: string }) => {
      const response = await apiClient.post<{ success: boolean; message: string; duration: number }>(
        `/integrations/${id}/test`,
        { message }
      )
      return response.data
    },
  })
}

export function useActivateIntegration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<Integration>(`/integrations/${id}/activate`)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['integrations'] })
    },
  })
}

export function useDeactivateIntegration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await apiClient.post<Integration>(`/integrations/${id}/deactivate`)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['integrations'] })
    },
  })
}

export function useDeleteIntegration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/integrations/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['integrations'] })
    },
  })
}

// ==================== AI Reasoning ====================

export interface RecommendedAction {
  action: string
  priority: string
  owner: string
}

export interface RootCauseAnalysis {
  id: string
  tenantId: string
  agentId: string
  traceId: string | null
  anomalyType: string
  anomalyStartTime: string
  anomalyEndTime: string
  probableRootCause: string
  confidence: number
  confidenceLabel: string
  evidence: string[]
  contributingFactors: string[]
  recommendedActions: RecommendedAction[]
  relatedTraceIds: string[]
  status: string
  createdAt: string
}

export function useTriggerRca() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: {
      agentId: string
      anomalyType: string
      anomalyStartTime: string
      anomalyEndTime: string
      traceId?: string
    }) => {
      const response = await apiClient.post<RootCauseAnalysis>('/ai-reasoning/root-cause-analysis', request)
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rcaResults'] })
    },
  })
}

export function useRcaHistory(agentId?: string) {
  return useQuery({
    queryKey: ['rcaResults', agentId],
    queryFn: async () => {
      if (agentId) {
        const response = await apiClient.get<RootCauseAnalysis[]>(`/ai-reasoning/root-cause-analysis/agent/${agentId}`)
        return response.data
      }
      return [] as RootCauseAnalysis[]
    },
    enabled: true,
  })
}

// ==================== Notifications Enhanced ====================

export function useUnreadNotifications() {
  return useQuery({
    queryKey: ['unreadNotifications'],
    queryFn: async () => {
      const response = await apiClient.get<Notification[]>('/notifications/unread')
      return response.data
    },
    refetchInterval: 30000,
  })
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.patch(`/notifications/${id}/read`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
      queryClient.invalidateQueries({ queryKey: ['unreadNotifications'] })
    },
  })
}

// ==================== Security Dashboard ====================

export interface SecurityDashboard {
  totalPolicies: number
  activePolicies: number
  totalViolations: number
  criticalViolations: number
  openViolations: number
  resolvedToday: number
  violationsBySeverity: { severity: string; count: number }[]
  violationsTrend: { date: string; count: number }[]
}

export function useSecurityDashboard() {
  return useQuery({
    queryKey: ['securityDashboard'],
    queryFn: async () => {
      const response = await apiClient.get<SecurityDashboard>('/security/dashboard')
      return response.data
    },
  })
}

export function useSecurityPolicies() {
  return useQuery({
    queryKey: ['securityPolicies'],
    queryFn: async () => {
      const response = await apiClient.get('/security/policies')
      return response.data
    },
  })
}
