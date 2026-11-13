import { useMemo } from 'react'
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine
} from 'recharts'
import { useCostTrend } from '@/hooks/useApi'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

interface CostTrendChartProps {
  agentId?: string
  days?: number
  showBudget?: boolean
  budgetAmount?: number
}

export function CostTrendChart({ 
  agentId, 
  days = 7, 
  showBudget = false,
  budgetAmount 
}: CostTrendChartProps) {
  const { data, isLoading } = useCostTrend({ agentId, days })

  const chartData = useMemo(() => {
    if (!data) return []
    return data.map(point => ({
      date: new Date(point.date).toLocaleDateString('en-US', { 
        month: 'short', 
        day: 'numeric' 
      }),
      cost: Number(point.cost ?? 0),
      budget: point.budget || 0
    }))
  }, [data])

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-48" />
        </CardHeader>
        <CardContent>
          <Skeleton className="h-[300px] w-full" />
        </CardContent>
      </Card>
    )
  }

  const totalCost = chartData.reduce((sum, d) => sum + d.cost, 0)
  const avgDaily = totalCost / chartData.length

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle className="text-base font-medium">Cost Trend</CardTitle>
          <p className="text-sm text-muted-foreground mt-1">
            £{totalCost.toFixed(2)} total • £{avgDaily.toFixed(2)} avg/day
          </p>
        </div>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <AreaChart data={chartData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
            <defs>
              <linearGradient id="costGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
                <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis 
              dataKey="date" 
              tick={{ fontSize: 12, fill: 'hsl(var(--muted-foreground))' }}
              axisLine={{ stroke: 'hsl(var(--border))' }}
              tickLine={false}
            />
            <YAxis 
              tick={{ fontSize: 12, fill: 'hsl(var(--muted-foreground))' }}
              axisLine={false}
              tickLine={false}
              tickFormatter={(value) => `£${value.toFixed(0)}`}
            />
            <Tooltip
              content={({ active, payload, label }) => {
                if (!active || !payload?.length) return null
                return (
                  <div className="rounded-lg border border-border bg-popover p-3 shadow-lg">
                    <p className="text-sm font-medium">{label}</p>
                    <p className="text-sm text-primary mt-1">
                      £{Number(payload[0].value).toFixed(4)}
                    </p>
                  </div>
                )
              }}
            />
            {showBudget && budgetAmount && (
              <ReferenceLine 
                y={budgetAmount / days} 
                stroke="hsl(var(--destructive))"
                strokeDasharray="5 5"
                label={{ 
                  value: 'Budget Limit', 
                  fill: 'hsl(var(--destructive))',
                  fontSize: 12 
                }}
              />
            )}
            <Area
              type="monotone"
              dataKey="cost"
              stroke="hsl(var(--primary))"
              strokeWidth={2}
              fill="url(#costGradient)"
              animationDuration={1000}
            />
          </AreaChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  )
}
