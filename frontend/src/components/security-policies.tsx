import { useState } from 'react'
import { Shield, Plus, Search, FileText, Power } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Select } from '@/components/ui/select'
import { PageLoader } from '@/components/ui/page-loader'
import {
  useSecurityPolicies,
  useCreateSecurityPolicy,
  useDisableSecurityPolicy,
  securityPolicyId,
  type SecurityPolicy,
} from '@/hooks/useApi'
import { FadeIn } from '@/components/ui/animations'

// Mirrors the backend enums. PolicyCategory, Severity, RuleType and AutoResponse.
const CATEGORIES = [
  'DataLossPrevention',
  'PromptInjection',
  'BehavioralAnomaly',
  'AccessControl',
  'RateLimiting',
  'DataProtection',
  'CostControl',
  'ResourceControl',
]

const SEVERITIES = ['Low', 'Medium', 'High', 'Critical']

const RULE_TYPES = ['RegexMatch', 'Threshold', 'Behavioral', 'Composite']

const AUTO_RESPONSES = ['SuspendAgent', 'BlockRequest', 'AlertOnly', 'Quarantine', 'AutoRespond']

// What each rule type needs in its expression and parameters, shown as a hint so
// the form does not depend on the reader already knowing the rule model.
const RULE_HINTS: Record<string, { expression: string; parameters: string }> = {
  RegexMatch: {
    expression: 'The field to test, e.g. operation_name or output_text',
    parameters: '{ "pattern": "the-regular-expression" }',
  },
  Threshold: {
    expression: 'The comparison, e.g. gt, gte, lt, lte or eq',
    parameters: '{ "metric": "duration_ms", "value": 5000 }',
  },
  Behavioral: {
    expression: 'The behaviour to detect, e.g. latency_drift',
    parameters: '{ "sigma": 3 }',
  },
  Composite: {
    expression: 'How the sub-rules combine, e.g. all, any or not',
    parameters: '{ "rules": [ { "type": "Threshold", "expression": "gt", "parameters": {} } ] }',
  },
}

const statusVariant = (status: string): 'success' | 'secondary' | 'default' =>
  status === 'Active' ? 'success' : status === 'Disabled' ? 'secondary' : 'default'

const severityVariant = (severity: string): 'destructive' | 'warning' | 'info' | 'default' => {
  const s = severity?.toLowerCase()
  if (s === 'critical') return 'destructive'
  if (s === 'high') return 'warning'
  if (s === 'low') return 'info'
  return 'default'
}

function ruleSummary(policy: SecurityPolicy): string {
  const rule = policy.ruleDefinition
  if (!rule) return 'Not recorded'
  const expression = rule.expression ? ` ${rule.expression}` : ''
  return `${rule.type ?? 'Rule'}${expression}`.trim()
}

export function SecurityPolicies() {
  const { data: policies, isLoading, error } = useSecurityPolicies()
  const createPolicy = useCreateSecurityPolicy()
  const disablePolicy = useDisableSecurityPolicy()

  const [searchTerm, setSearchTerm] = useState('')
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [form, setForm] = useState({
    name: '',
    description: '',
    category: 'DataProtection',
    severity: 'Medium',
    ruleType: 'Threshold',
    expression: 'gt',
    parameters: '{ "metric": "duration_ms", "value": 5000 }',
    autoResponse: 'AlertOnly',
  })

  const list = Array.isArray(policies) ? policies : []

  const filtered = list.filter(p => {
    const term = searchTerm.toLowerCase()
    return (
      p.name.toLowerCase().includes(term) ||
      (p.description ?? '').toLowerCase().includes(term) ||
      p.category.toLowerCase().includes(term)
    )
  })

  const handleCreate = async () => {
    // Parameters are edited as JSON; an empty box means an empty parameter set.
    let parameters: Record<string, unknown> = {}
    if (form.parameters.trim()) {
      try {
        parameters = JSON.parse(form.parameters)
      } catch {
        // Surface the parse failure rather than sending a malformed rule.
        window.alert('Parameters must be valid JSON.')
        return
      }
    }

    await createPolicy.mutateAsync({
      name: form.name.trim(),
      description: form.description.trim(),
      category: form.category,
      severity: form.severity,
      ruleDefinition: { type: form.ruleType, expression: form.expression.trim(), parameters },
      autoResponse: form.autoResponse,
    })

    setShowCreateForm(false)
    setForm({
      name: '',
      description: '',
      category: 'DataProtection',
      severity: 'Medium',
      ruleType: 'Threshold',
      expression: 'gt',
      parameters: '{ "metric": "duration_ms", "value": 5000 }',
      autoResponse: 'AlertOnly',
    })
  }

  if (isLoading) {
    return <PageLoader label="Loading security policies" />
  }

  if (error) {
    return (
      <Card>
        <CardContent className="py-12 text-center">
          <p className="text-destructive">Failed to load security policies</p>
          <p className="text-sm text-muted-foreground mt-2">Please check your backend connection</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <FadeIn>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="font-display text-2xl font-semibold">Security Policies</h2>
            <p className="text-sm text-muted-foreground mt-1">
              Policies are evaluated against ingested traces. Disabling a policy stops
              evaluation; the record is kept.
            </p>
          </div>
          <Button onClick={() => setShowCreateForm(v => !v)}>
            <Plus className="w-4 h-4 mr-2" />
            Create Policy
          </Button>
        </div>

        {showCreateForm && (
          <Card className="glass-card">
            <CardHeader>
              <CardTitle className="text-lg">Create Security Policy</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium text-muted-foreground">Name</label>
                  <Input
                    value={form.name}
                    onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                    placeholder="Policy name"
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium text-muted-foreground">Category</label>
                  <Select
                    value={form.category}
                    onValueChange={v => setForm(f => ({ ...f, category: v }))}
                    options={CATEGORIES.map(c => ({ value: c, label: c }))}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-muted-foreground">Description</label>
                <Input
                  value={form.description}
                  onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                  placeholder="What this policy detects"
                />
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium text-muted-foreground">Rule type</label>
                  <Select
                    value={form.ruleType}
                    onValueChange={v => setForm(f => ({ ...f, ruleType: v }))}
                    options={RULE_TYPES.map(t => ({ value: t, label: t }))}
                  />
                  <p className="text-xs text-muted-foreground">
                    Expression: {RULE_HINTS[form.ruleType].expression}
                  </p>
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium text-muted-foreground">Severity</label>
                  <Select
                    value={form.severity}
                    onValueChange={v => setForm(f => ({ ...f, severity: v }))}
                    options={SEVERITIES.map(s => ({ value: s, label: s }))}
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium text-muted-foreground">Rule expression</label>
                  <Input
                    value={form.expression}
                    onChange={e => setForm(f => ({ ...f, expression: e.target.value }))}
                    className="font-mono"
                    placeholder={RULE_HINTS[form.ruleType].expression}
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium text-muted-foreground">
                    Rule parameters (JSON)
                  </label>
                  <Input
                    value={form.parameters}
                    onChange={e => setForm(f => ({ ...f, parameters: e.target.value }))}
                    className="font-mono text-xs"
                    placeholder={RULE_HINTS[form.ruleType].parameters}
                  />
                  <p className="text-xs text-muted-foreground">
                    Example: {RULE_HINTS[form.ruleType].parameters}
                  </p>
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-muted-foreground">Auto response</label>
                <Select
                  value={form.autoResponse}
                  onValueChange={v => setForm(f => ({ ...f, autoResponse: v }))}
                  options={AUTO_RESPONSES.map(a => ({ value: a, label: a }))}
                />
              </div>

              <div className="flex gap-2">
                <Button
                  onClick={handleCreate}
                  disabled={createPolicy.isPending || !form.name.trim() || !form.expression.trim()}
                >
                  {createPolicy.isPending ? 'Creating...' : 'Create Policy'}
                </Button>
                <Button variant="outline" onClick={() => setShowCreateForm(false)}>Cancel</Button>
              </div>
              {createPolicy.isError && (
                <p className="text-sm text-destructive">
                  Failed to create the policy. Check the rule definition and try again.
                </p>
              )}
            </CardContent>
          </Card>
        )}

        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-10"
            placeholder="Search policies..."
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
          />
        </div>

        {filtered.length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center text-muted-foreground">
              <Shield className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No policies found</p>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-3">
            {filtered.map(policy => {
              const id = securityPolicyId(policy)
              const isOpen = expandedId === id
              const isActive = policy.status === 'Active'
              return (
                <Card key={id} className="glass-card">
                  <CardContent className="p-5">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-3 mb-1 flex-wrap">
                          <h3 className="font-semibold text-foreground">{policy.name}</h3>
                          <Badge variant={statusVariant(policy.status)}>{policy.status}</Badge>
                          <Badge variant={severityVariant(policy.severity)}>{policy.severity}</Badge>
                          <span className="text-xs text-muted-foreground">{policy.category}</span>
                        </div>
                        {policy.description && (
                          <p className="text-sm text-muted-foreground">{policy.description}</p>
                        )}
                        <p className="font-mono text-xs text-muted-foreground mt-2">
                          {ruleSummary(policy)}
                          {policy.autoResponse ? ` · ${policy.autoResponse}` : ''}
                        </p>
                      </div>
                      <div className="flex gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => setExpandedId(isOpen ? null : id)}
                        >
                          <FileText className="w-4 h-4 mr-1" />
                          {isOpen ? 'Hide' : 'View'}
                        </Button>
                        {isActive && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => disablePolicy.mutate(securityPolicyId(policy))}
                            disabled={disablePolicy.isPending}
                          >
                            <Power className="w-4 h-4 mr-1" />
                            Disable
                          </Button>
                        )}
                      </div>
                    </div>

                    {isOpen && (
                      <div className="mt-4 space-y-3 border-t border-border pt-4">
                        <div>
                          <p className="text-xs font-medium text-muted-foreground mb-1">Rule definition</p>
                          <pre className="rounded-xl bg-muted/40 p-3 text-xs font-mono overflow-x-auto">
                            {JSON.stringify(policy.ruleDefinition ?? 'Not recorded', null, 2)}
                          </pre>
                        </div>
                        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 text-xs">
                          <div>
                            <p className="text-muted-foreground">Created</p>
                            <p className="font-medium">{new Date(policy.createdAt).toLocaleString()}</p>
                          </div>
                          <div>
                            <p className="text-muted-foreground">Updated</p>
                            <p className="font-medium">
                              {policy.updatedAt ? new Date(policy.updatedAt).toLocaleString() : 'Never'}
                            </p>
                          </div>
                          <div>
                            <p className="text-muted-foreground">Policy ID</p>
                            <p className="font-mono">{id}</p>
                          </div>
                          <div>
                            <p className="text-muted-foreground">Auto response</p>
                            <p className="font-medium">{policy.autoResponse ?? 'None'}</p>
                          </div>
                        </div>
                      </div>
                    )}
                  </CardContent>
                </Card>
              )
            })}
          </div>
        )}

      </div>
    </FadeIn>
  )
}
