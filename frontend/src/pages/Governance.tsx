import { useState } from 'react'
import { Shield, Plus, CheckCircle2, Clock, FileText, Search } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Skeleton } from '@/components/ui/skeleton'
import { FadeIn } from '@/components/ui/animations'
import { useGovernancePolicies, useCreatePolicy, useActivatePolicy } from '@/hooks/useApi'

const categoryColors: Record<string, string> = {
  DataAccess: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
  ModelUsage: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400',
  CostControl: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  Security: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
  EthicalGuidelines: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400',
  Operational: 'bg-slate-100 text-slate-800 dark:bg-slate-900/30 dark:text-slate-400',
}

const statusColors: Record<string, string> = {
  Draft: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  Active: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  Deprecated: 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400',
}

export function GovernancePage() {
  const { data: policies, isLoading } = useGovernancePolicies()
  const createPolicy = useCreatePolicy()
  const activatePolicy = useActivatePolicy()
  const [searchTerm, setSearchTerm] = useState('')
  const [activeTab, setActiveTab] = useState<'all' | 'draft' | 'active' | 'deprecated'>('all')
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [newPolicy, setNewPolicy] = useState({ name: '', description: '', category: 'Security', definitionYaml: 'rules:\n  - name: default\n    action: allow' })

  const filtered = (policies ?? []).filter(p => {
    const matchesSearch = p.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      p.description.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesTab = activeTab === 'all' || p.status.toLowerCase() === activeTab
    return matchesSearch && matchesTab
  })

  const handleCreate = async () => {
    await createPolicy.mutateAsync(newPolicy)
    setShowCreateForm(false)
    setNewPolicy({ name: '', description: '', category: 'Security', definitionYaml: 'rules:\n  - name: default\n    action: allow' })
  }

  const handleActivate = async (id: string) => {
    await activatePolicy.mutateAsync(id)
  }

  return (
    <FadeIn>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Governance Policies</h1>
            <p className="page-subtitle">Manage AI agent governance policies and compliance rules</p>
          </div>
          <Button onClick={() => setShowCreateForm(!showCreateForm)}>
            <Plus className="w-4 h-4 mr-2" />
            Create Policy
          </Button>
        </div>

        {showCreateForm && (
          <Card className="glass-card">
            <CardHeader>
              <CardTitle className="text-lg">Create New Policy</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Name</label>
                  <Input value={newPolicy.name} onChange={e => setNewPolicy(p => ({ ...p, name: e.target.value }))} placeholder="Policy name" />
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Category</label>
                  <select
                    className="w-full h-10 px-3 rounded-md border bg-background text-foreground"
                    value={newPolicy.category}
                    onChange={e => setNewPolicy(p => ({ ...p, category: e.target.value }))}
                  >
                    {Object.keys(categoryColors).map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Description</label>
                <Input value={newPolicy.description} onChange={e => setNewPolicy(p => ({ ...p, description: e.target.value }))} placeholder="What this policy enforces" />
              </div>
              <div>
                <label className="text-sm font-medium text-muted-foreground">Definition (YAML)</label>
                <textarea
                  className="w-full h-32 px-3 py-2 rounded-md border bg-background text-foreground font-mono text-sm"
                  value={newPolicy.definitionYaml}
                  onChange={e => setNewPolicy(p => ({ ...p, definitionYaml: e.target.value }))}
                />
              </div>
              <div className="flex gap-2">
                <Button onClick={handleCreate} disabled={createPolicy.isPending || !newPolicy.name}>
                  {createPolicy.isPending ? 'Creating...' : 'Create Policy'}
                </Button>
                <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
              </div>
            </CardContent>
          </Card>
        )}

        <div className="flex items-center gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input className="pl-9" placeholder="Search policies..." value={searchTerm} onChange={e => setSearchTerm(e.target.value)} />
          </div>
          <Tabs defaultValue="all" value={activeTab} onValueChange={(v) => setActiveTab(v as typeof activeTab)}>
            <TabsList>
              <TabsTrigger value="all">All</TabsTrigger>
              <TabsTrigger value="draft">Draft</TabsTrigger>
              <TabsTrigger value="active">Active</TabsTrigger>
              <TabsTrigger value="deprecated">Deprecated</TabsTrigger>
            </TabsList>
          </Tabs>
        </div>

        {isLoading ? (
          <div className="space-y-4">
            {[1, 2, 3].map(i => <Skeleton key={i} className="h-32 w-full rounded-xl" />)}
          </div>
        ) : filtered.length === 0 ? (
          <Card className="glass-card">
            <CardContent className="py-12 text-center text-muted-foreground">
              <Shield className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No policies found</p>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4">
            {filtered.map(policy => (
              <Card key={policy.id} className="glass-card hover:border-primary/20 transition-colors">
                <CardContent className="p-5">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <h3 className="font-semibold text-foreground">{policy.name}</h3>
                        <Badge className={categoryColors[policy.category] ?? ''}>{policy.category}</Badge>
                        <Badge className={statusColors[policy.status] ?? ''}>{policy.status}</Badge>
                        <span className="text-xs text-muted-foreground">v{policy.version}</span>
                      </div>
                      <p className="text-sm text-muted-foreground mb-3">{policy.description}</p>
                      <div className="flex items-center gap-4 text-xs text-muted-foreground">
                        <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> Created {new Date(policy.createdAt).toLocaleDateString()}</span>
                        {policy.activatedAt && <span className="flex items-center gap-1"><CheckCircle2 className="w-3 h-3" /> Activated {new Date(policy.activatedAt).toLocaleDateString()}</span>}
                      </div>
                    </div>
                    <div className="flex gap-2">
                      {policy.status === 'Draft' && (
                        <Button size="sm" onClick={() => handleActivate(policy.id)} disabled={activatePolicy.isPending}>
                          Activate
                        </Button>
                      )}
                      <Button size="sm" variant="outline">
                        <FileText className="w-4 h-4 mr-1" /> View
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </FadeIn>
  )
}
