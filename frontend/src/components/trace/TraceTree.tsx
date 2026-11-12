import { useState, useMemo, useCallback } from 'react'
import {
  ChevronRight,
  ChevronDown,
  Clock,
  AlertCircle,
  CheckCircle2,
  AlertTriangle,
  Cpu,
  Boxes,
  Zap,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { Trace } from '@/hooks/useApi'

export interface TraceTreeNode extends Trace {
  children?: TraceTreeNode[]
}

interface TraceTreeProps {
  root?: Trace
  traces?: Trace[]
  selectedTraceId?: string | null
  onSelectTrace?: (trace: Trace) => void
  className?: string
}

function buildTreeFromList(traces: Trace[], rootId?: string): TraceTreeNode | null {
  if (!traces || traces.length === 0) return null

  // If rootId is provided, find that root; otherwise find trace with no parent or earliest
  let root = rootId ? traces.find(t => t.id === rootId) : undefined
  if (!root) {
    root = traces.find(t => !t.parentTraceId) || traces[0]
  }

  const map = new Map<string, TraceTreeNode>()
  traces.forEach(t => {
    map.set(t.id, { ...t, children: [...(t.children || [])] })
  })

  // Wire children if not already wired
  traces.forEach(t => {
    if (t.parentTraceId && map.has(t.parentTraceId)) {
      const parent = map.get(t.parentTraceId)!
      if (!parent.children) parent.children = []
      if (!parent.children.some(c => c.id === t.id)) {
        parent.children.push(map.get(t.id)!)
      }
    }
  })

  return map.get(root.id) || null
}

export function TraceTree({
  root,
  traces,
  selectedTraceId,
  onSelectTrace,
  className,
}: TraceTreeProps) {
  // Determine root node
  const treeRoot = useMemo(() => {
    if (root) {
      // If root has children already
      if (root.children && root.children.length > 0) {
        return root as TraceTreeNode
      }
      // If traces list is also provided, build tree
      if (traces && traces.length > 0) {
        return buildTreeFromList(traces, root.id)
      }
      return root as TraceTreeNode
    }
    if (traces && traces.length > 0) {
      return buildTreeFromList(traces)
    }
    return null
  }, [root, traces])

  // Track expanded nodes
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(() => {
    const initial = new Set<string>()
    if (treeRoot) {
      initial.add(treeRoot.id)
      // Expand immediate children too
      treeRoot.children?.forEach(c => initial.add(c.id))
    }
    return initial
  })

  const toggleNode = useCallback((nodeId: string) => {
    setExpandedNodes(prev => {
      const next = new Set(prev)
      if (next.has(nodeId)) {
        next.delete(nodeId)
      } else {
        next.add(nodeId)
      }
      return next
    })
  }, [])

  if (!treeRoot) {
    return (
      <div className="p-8 text-center text-muted-foreground border border-dashed rounded-xl">
        <Boxes className="h-8 w-8 mx-auto mb-2 opacity-30" />
        <p className="text-sm">No span hierarchy available</p>
      </div>
    )
  }

  const maxDuration = Math.max(treeRoot.durationMs || 1, 1)

  const renderNode = (node: TraceTreeNode, depth: number = 0): JSX.Element => {
    const isExpanded = expandedNodes.has(node.id)
    const hasChildren = Boolean(node.children && node.children.length > 0)
    const isSelected = selectedTraceId === node.id

    const isError = node.status === 'error'
    const isDegraded = node.status === 'degraded'

    const statusIcon = isError ? (
      <AlertCircle className="h-4 w-4 text-red-400 flex-shrink-0" />
    ) : isDegraded ? (
      <AlertTriangle className="h-4 w-4 text-amber-400 flex-shrink-0" />
    ) : (
      <CheckCircle2 className="h-4 w-4 text-emerald-400 flex-shrink-0" />
    )

    const durationPercent = Math.min(100, Math.max(5, (node.durationMs / maxDuration) * 100))

    return (
      <div key={node.id} className="select-none text-xs">
        <div
          className={cn(
            'group relative flex items-center gap-2.5 py-2.5 px-3 rounded-xl cursor-pointer transition-all duration-150',
            isSelected
              ? 'bg-primary/[0.08] border border-primary/40 shadow-sm text-foreground'
              : 'hover:bg-muted/40 border border-transparent text-muted-foreground hover:text-foreground',
            depth === 0 && 'font-medium'
          )}
          style={{ paddingLeft: `${depth * 20 + 12}px` }}
          onClick={() => onSelectTrace?.(node)}
        >
          {/* Collapse/Expand chevron */}
          {hasChildren ? (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation()
                toggleNode(node.id)
              }}
              className="p-1 -ml-1 rounded-md hover:bg-muted/60 text-muted-foreground hover:text-foreground transition-colors"
            >
              {isExpanded ? (
                <ChevronDown className="h-3.5 w-3.5" />
              ) : (
                <ChevronRight className="h-3.5 w-3.5" />
              )}
            </button>
          ) : (
            <span className="w-4" />
          )}

          {/* Status icon */}
          {statusIcon}

          {/* Operation Name + Subtitle */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="font-mono font-medium text-foreground truncate text-xs sm:text-sm">
                {node.operationName}
              </span>

              {/* Type Badge */}
              {node.operationType && (
                <span className="px-1.5 py-0.5 rounded text-[10px] uppercase font-mono tracking-wider bg-white/5 border border-white/10 text-muted-foreground">
                  {node.operationType}
                </span>
              )}

              {/* Model chip */}
              {node.modelName && node.modelName !== 'unknown' && (
                <span className="hidden sm:inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] bg-primary/10 text-primary border border-primary/20">
                  <Cpu size={10} />
                  {node.modelName}
                </span>
              )}
            </div>

            {/* Child count & agent name */}
            <div className="flex items-center gap-2 text-[11px] text-muted-foreground/70 mt-0.5">
              <span>{node.agentName || node.agentId.slice(0, 8)}</span>
              {hasChildren && (
                <span>&bull; {node.children!.length} {node.children!.length === 1 ? 'sub-span' : 'sub-spans'}</span>
              )}
            </div>
          </div>

          {/* Latency progress bar + metrics */}
          <div className="flex items-center gap-3 text-[11px] flex-shrink-0">
            {/* Visual latency bar */}
            <div className="hidden md:flex flex-col items-end gap-1 w-24">
              <div className="w-full bg-white/5 h-1.5 rounded-full overflow-hidden">
                <div
                  className={cn(
                    'h-full rounded-full transition-all duration-300',
                    isError ? 'bg-red-500' : 'bg-emerald-500'
                  )}
                  style={{ width: `${durationPercent}%` }}
                />
              </div>
            </div>

            {/* Duration */}
            <span className="flex items-center gap-1 font-mono text-muted-foreground">
              <Clock className="h-3 w-3 opacity-60" />
              {node.durationMs}ms
            </span>

            {/* Tokens */}
            {node.tokenTotal > 0 && (
              <span className="hidden sm:inline-flex items-center gap-0.5 font-mono text-muted-foreground/80">
                <Zap className="h-3 w-3 text-amber-400 opacity-80" />
                {node.tokenTotal.toLocaleString()}
              </span>
            )}

            {/* Cost */}
            {node.costUsd > 0 && (
              <span className="font-mono text-emerald-400 font-medium">
                ${node.costUsd.toFixed(4)}
              </span>
            )}
          </div>
        </div>

        {/* Children sub-tree */}
        {isExpanded && hasChildren && (
          <div className="relative border-l border-white/10 ml-5 pl-1 my-1 space-y-1">
            {node.children!.map(child => renderNode(child, depth + 1))}
          </div>
        )}
      </div>
    )
  }

  return (
    <div className={cn('rounded-2xl border border-border bg-card/60 backdrop-blur-sm overflow-hidden shadow-sm', className)}>
      <div className="px-4 py-3 border-b border-border bg-muted/40 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Boxes className="h-4 w-4 text-primary" />
          <h3 className="text-xs font-semibold uppercase tracking-wider text-foreground font-mono">
            Execution Decision Tree
          </h3>
        </div>
        <div className="flex items-center gap-3 text-xs text-muted-foreground font-mono">
          <span>Total Latency: {treeRoot.durationMs}ms</span>
          {treeRoot.tokenTotal > 0 && <span>&bull; {treeRoot.tokenTotal.toLocaleString()} tokens</span>}
        </div>
      </div>
      <div className="p-3 max-h-[640px] overflow-y-auto space-y-1">
        {renderNode(treeRoot)}
      </div>
    </div>
  )
}
