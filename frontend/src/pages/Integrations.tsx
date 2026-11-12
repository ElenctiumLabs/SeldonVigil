import { useState } from 'react'
import { Plug, Plus, Trash2, Power, PowerOff, TestTube, CheckCircle2, XCircle, AlertTriangle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { FadeIn } from '@/components/ui/animations'
import {
  useIntegrations, useCreateIntegration, useTestIntegration,
  useActivateIntegration, useDeactivateIntegration, useDeleteIntegration,
  type Integration
} from '@/hooks/useApi'

const typeIcons: Record<string, string> = {
  Slack: '#4A154B',
  Teams: '#6264A7',
  PagerDuty: '#06AC38',
  Jira: '#0052CC',
  GitHub: '#24292e',
  Webhook: '#FF6B35',
  Email: '#EA4335',
}

const typeLabels: Record<string, string> = {
  Slack: 'Slack',
  Teams: 'Microsoft Teams',
  PagerDuty: 'PagerDuty',
  Jira: 'Jira',
  GitHub: 'GitHub',
  Webhook: 'Webhook',
  Email: 'Email',
}

function IntegrationCard({ integration }: { integration: Integration }) {
  const testIntegration = useTestIntegration()
  const activateIntegration = useActivateIntegration()
  const deactivateIntegration = useDeactivateIntegration()
  const deleteIntegration = useDeleteIntegration()
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null)

  const handleTest = async () => {
    setTestResult(null)
    const result = await testIntegration.mutateAsync({ id: integration.id })
    setTestResult(result)
  }

  const handleToggle = async () => {
    if (integration.status === 'Active') {
      await deactivateIntegration.mutateAsync(integration.id)
    } else {
      await activateIntegration.mutateAsync(integration.id)
    }
  }

  const handleDelete = async () => {
    if (confirm(`Delete integration "${integration.name}"?`)) {
      await deleteIntegration.mutateAsync(integration.id)
    }
  }

  return (
    <Card className="glass-card hover:border-primary/20 transition-colors">
      <CardContent className="p-5">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-4">
            <div
              className="w-12 h-12 rounded-xl flex items-center justify-center text-white text-lg font-bold"
              style={{ backgroundColor: typeIcons[integration.type] ?? '#6B7280' }}
            >
              {integration.type[0]}
            </div>
            <div>
              <h3 className="font-semibold text-foreground">{integration.name}</h3>
              <p className="text-sm text-muted-foreground">{typeLabels[integration.type] ?? integration.type}</p>
              <div className="flex items-center gap-3 mt-1">
                <Badge variant={integration.status === 'Active' ? 'default' : integration.status === 'Error' ? 'destructive' : 'secondary'}>
                  {integration.status}
                </Badge>
                {integration.errorCount > 0 && (
                  <span className="text-xs text-red-500 flex items-center gap-1">
                    <AlertTriangle className="w-3 h-3" /> {integration.errorCount} errors
                  </span>
                )}
                {integration.lastUsedAt && (
                  <span className="text-xs text-muted-foreground">
                    Last used {new Date(integration.lastUsedAt).toLocaleDateString()}
                  </span>
                )}
              </div>
            </div>
          </div>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={handleTest} disabled={testIntegration.isPending}>
              <TestTube className="w-4 h-4 mr-1" /> Test
            </Button>
            <Button size="sm" variant="outline" onClick={handleToggle}>
              {integration.status === 'Active' ? <PowerOff className="w-4 h-4" /> : <Power className="w-4 h-4" />}
            </Button>
            <Button size="sm" variant="outline" onClick={handleDelete}>
              <Trash2 className="w-4 h-4 text-red-500" />
            </Button>
          </div>
        </div>
        {testResult && (
          <div className={`mt-3 p-3 rounded-md text-sm flex items-center gap-2 ${testResult.success ? 'bg-green-50 text-green-800 dark:bg-green-950/30 dark:text-green-400' : 'bg-red-50 text-red-800 dark:bg-red-950/30 dark:text-red-400'}`}>
            {testResult.success ? <CheckCircle2 className="w-4 h-4" /> : <XCircle className="w-4 h-4" />}
            {testResult.message}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

export function IntegrationsPage() {
  const { data: integrations, isLoading } = useIntegrations()
  const createIntegration = useCreateIntegration()
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [newIntegration, setNewIntegration] = useState({ name: '', type: 'Slack', configurationJson: '{}', webhookUrl: '', apiKey: '' })

  const handleCreate = async () => {
    await createIntegration.mutateAsync({
      name: newIntegration.name,
      type: newIntegration.type,
      configurationJson: newIntegration.configurationJson,
      webhookUrl: newIntegration.webhookUrl || undefined,
      apiKey: newIntegration.apiKey || undefined,
    })
    setShowCreateForm(false)
    setNewIntegration({ name: '', type: 'Slack', configurationJson: '{}', webhookUrl: '', apiKey: '' })
  }

  return (
    <FadeIn>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Integrations</h1>
            <p className="page-subtitle">Connect external services for notifications and workflows</p>
          </div>
          <Button onClick={() => setShowCreateForm(!showCreateForm)}>
            <Plus className="w-4 h-4 mr-2" />
            Add Integration
          </Button>
        </div>

        {showCreateForm && (
          <Card className="glass-card">
            <CardHeader>
              <CardTitle className="text-lg">Add New Integration</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Name</label>
                  <Input value={newIntegration.name} onChange={e => setNewIntegration(p => ({ ...p, name: e.target.value }))} placeholder="My Slack Workspace" />
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Type</label>
                  <select
                    className="w-full h-10 px-3 rounded-md border bg-background text-foreground"
                    value={newIntegration.type}
                    onChange={e => setNewIntegration(p => ({ ...p, type: e.target.value }))}
                  >
                    {Object.keys(typeLabels).map(t => <option key={t} value={t}>{typeLabels[t]}</option>)}
                  </select>
                </div>
              </div>
              {(newIntegration.type === 'Webhook' || newIntegration.type === 'Slack') && (
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Webhook URL</label>
                  <Input value={newIntegration.webhookUrl} onChange={e => setNewIntegration(p => ({ ...p, webhookUrl: e.target.value }))} placeholder="https://hooks.slack.com/..." />
                </div>
              )}
              <div className="flex gap-2">
                <Button onClick={handleCreate} disabled={createIntegration.isPending || !newIntegration.name}>
                  {createIntegration.isPending ? 'Creating...' : 'Create Integration'}
                </Button>
                <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
              </div>
            </CardContent>
          </Card>
        )}

        {isLoading ? (
          <div className="space-y-4">
            {[1, 2, 3].map(i => <Skeleton key={i} className="h-28 w-full rounded-xl" />)}
          </div>
        ) : !integrations || integrations.length === 0 ? (
          <Card className="glass-card">
            <CardContent className="py-12 text-center text-muted-foreground">
              <Plug className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No integrations configured</p>
              <p className="text-sm mt-1">Connect Slack, Teams, PagerDuty, and more</p>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            {integrations.map(i => <IntegrationCard key={i.id} integration={i} />)}
          </div>
        )}
      </div>
    </FadeIn>
  )
}
