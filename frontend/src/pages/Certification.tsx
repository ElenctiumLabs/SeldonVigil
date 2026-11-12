import { useState } from 'react'
import { GitBranch, Play, CheckCircle2, XCircle, Clock } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { FadeIn } from '@/components/ui/animations'
import { useCertificationPipelines, useTriggerCertification, useApproveCertification, type CertificationPipeline } from '@/hooks/useApi'

const stageStatusIcon = (status: string) => {
  switch (status) {
    case 'Passed': return <CheckCircle2 className="w-4 h-4 text-green-500" />
    case 'Failed': return <XCircle className="w-4 h-4 text-red-500" />
    case 'InProgress': return <Clock className="w-4 h-4 text-blue-500 animate-pulse" />
    default: return <div className="w-4 h-4 rounded-full border-2 border-muted-foreground/30" />
  }
}

const pipelineStatusColor = (status: string) => {
  switch (status) {
    case 'Approved': return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
    case 'Rejected': return 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
    case 'Pending': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400'
    case 'AwaitingApproval': return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400'
    default: return 'bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400'
  }
}

function PipelineCard({ pipeline }: { pipeline: CertificationPipeline }) {
  const approveCert = useApproveCertification()

  const handleApprove = async () => {
    await approveCert.mutateAsync({ id: pipeline.id, notes: 'Approved via dashboard' })
  }

  return (
    <Card className="glass-card">
      <CardContent className="p-5">
        <div className="flex items-start justify-between mb-4">
          <div>
            <div className="flex items-center gap-3 mb-1">
              <h3 className="font-semibold text-foreground">Pipeline</h3>
              <Badge className={pipelineStatusColor(pipeline.status)}>{pipeline.status}</Badge>
            </div>
            <p className="text-sm text-muted-foreground">Agent</p>
          </div>
          {pipeline.status === 'AwaitingApproval' && (
            <Button size="sm" onClick={handleApprove} disabled={approveCert.isPending}>
              Approve
            </Button>
          )}
        </div>

        <div className="space-y-3">
          {pipeline.stages.map((stage) => (
            <div key={stage.name} className="flex items-center gap-3">
              <div className="flex items-center gap-2 w-48">
                {stageStatusIcon(stage.status)}
                <span className={`text-sm ${stage.status === 'InProgress' ? 'font-medium text-foreground' : 'text-muted-foreground'}`}>
                  {stage.description}
                </span>
              </div>
              {stage.isRequired && <Badge variant="outline" className="text-xs">Required</Badge>}
              {stage.completedAt && (
                <span className="text-xs text-muted-foreground ml-auto">
                  {new Date(stage.completedAt).toLocaleTimeString()}
                </span>
              )}
            </div>
          ))}
        </div>

        <div className="flex items-center gap-4 mt-4 text-xs text-muted-foreground">
          <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> Started {new Date(pipeline.startedAt).toLocaleString()}</span>
          {pipeline.completedAt && <span>Completed {new Date(pipeline.completedAt).toLocaleString()}</span>}
          {pipeline.approvalNotes && <span className="italic">"{pipeline.approvalNotes}"</span>}
        </div>
      </CardContent>
    </Card>
  )
}

export function CertificationPage() {
  const { data: pipelines, isLoading } = useCertificationPipelines()
  const triggerCert = useTriggerCertification()
  const [agentId, setAgentId] = useState('')

  const handleTrigger = async () => {
    if (!agentId) return
    await triggerCert.mutateAsync(agentId)
    setAgentId('')
  }

  return (
    <FadeIn>
      <div className="space-y-6">
        <div>
          <h1 className="page-title font-display text-4xl gradient-text">Certification Pipelines</h1>
          <p className="page-subtitle">Manage agent certification workflows and approvals</p>
        </div>

        <Card className="glass-card">
          <CardHeader>
            <CardTitle className="text-lg">Trigger New Pipeline</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex gap-3">
              <Input
                placeholder="Agent ID"
                value={agentId}
                onChange={e => setAgentId(e.target.value)}
                className="flex-1"
              />
              <Button onClick={handleTrigger} disabled={triggerCert.isPending || !agentId}>
                <Play className="w-4 h-4 mr-2" />
                {triggerCert.isPending ? 'Triggering...' : 'Trigger Pipeline'}
              </Button>
            </div>
          </CardContent>
        </Card>

        {isLoading ? (
          <div className="space-y-4">
            {[1, 2].map(i => <Skeleton key={i} className="h-48 w-full rounded-xl" />)}
          </div>
        ) : !pipelines || pipelines.length === 0 ? (
          <Card className="glass-card">
            <CardContent className="py-12 text-center text-muted-foreground">
              <GitBranch className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No certification pipelines yet</p>
              <p className="text-sm mt-1">Trigger a pipeline for an agent above</p>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            {pipelines.map(p => <PipelineCard key={p.id} pipeline={p} />)}
          </div>
        )}
      </div>
    </FadeIn>
  )
}
