import { useState, useEffect } from 'react'
import { Bell, X, Shield, DollarSign, AlertTriangle, Info, Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useNotifications } from '@/hooks/useApi'
import { Skeleton } from './skeleton'

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

interface NotificationDrawerProps {
  open: boolean
  onClose: () => void
}

export function NotificationDrawer({ open, onClose }: NotificationDrawerProps) {
  const [localNotifications, setLocalNotifications] = useState<Notification[]>([])
  const [activeTab, setActiveTab] = useState<'all' | 'unread'>('all')

  const { data: apiNotifications, isLoading, error } = useNotifications()

  // Sync API data with local state for optimistic updates
  useEffect(() => {
    if (apiNotifications) {
      setLocalNotifications(apiNotifications)
    }
  }, [apiNotifications])

  useEffect(() => {
    if (open) {
      document.body.style.overflow = 'hidden'
    }
    return () => {
      document.body.style.overflow = ''
    }
  }, [open])

  const unreadCount = localNotifications.filter(n => !n.read).length
  const filteredNotifications = activeTab === 'unread'
    ? localNotifications.filter(n => !n.read)
    : localNotifications

  const markAsRead = (id: string) => {
    setLocalNotifications(prev => prev.map(n => n.id === id ? { ...n, read: true } : n))
  }

  const markAllAsRead = () => {
    setLocalNotifications(prev => prev.map(n => ({ ...n, read: true })))
  }

  const icons: Record<NotificationType, typeof Bell> = {
    security_alert: Shield,
    cost_anomaly: DollarSign,
    policy_violation: AlertTriangle,
    system: Info,
  }

  if (!open) return null

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50 z-40 lg:hidden"
        onClick={onClose}
      />

      {/* Drawer */}
      <div className="fixed inset-y-0 right-0 w-full sm:w-96 bg-background border-l border-border z-50 flex flex-col animate-fade-in">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border">
          <div className="flex items-center gap-3">
            <Bell size={20} className="text-primary" />
            <h2 className="text-lg font-semibold">Notifications</h2>
            {unreadCount > 0 && (
              <span className="px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium">
                {unreadCount} new
              </span>
            )}
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-muted transition-colors"
          >
            <X size={18} />
          </button>
        </div>

        {/* Loading State */}
        {isLoading && (
          <div className="flex-1 p-3 space-y-2">
            {[1, 2, 3, 4].map(i => (
              <Skeleton key={i} className="h-16 w-full rounded-xl" />
            ))}
          </div>
        )}

        {/* Error State */}
        {error && (
          <div className="flex-1 flex items-center justify-center p-4">
            <p className="text-sm text-muted-foreground">Failed to load notifications</p>
          </div>
        )}

        {/* Content */}
        {!isLoading && !error && (
          <>
            {/* Tabs */}
            <div className="p-3 border-b border-border">
              <div className="inline-flex items-center gap-1 p-1 bg-muted/50 rounded-xl">
                <button
                  onClick={() => setActiveTab('all')}
                  className={cn(
                    'px-3 py-1.5 rounded-lg text-sm font-medium transition-all',
                    activeTab === 'all' ? 'bg-background shadow-sm' : 'text-muted-foreground hover:text-foreground'
                  )}
                >
                  All
                </button>
                <button
                  onClick={() => setActiveTab('unread')}
                  className={cn(
                    'px-3 py-1.5 rounded-lg text-sm font-medium transition-all',
                    activeTab === 'unread' ? 'bg-background shadow-sm' : 'text-muted-foreground hover:text-foreground'
                  )}
                >
                  Unread
                </button>
              </div>
            </div>

            {/* Actions */}
            {unreadCount > 0 && (
              <div className="px-4 py-2">
                <button
                  onClick={markAllAsRead}
                  className="text-sm font-medium text-primary hover:text-primary/80 transition-colors"
                >
                  Mark all as read
                </button>
              </div>
            )}

            {/* Notifications List */}
            <div className="flex-1 overflow-y-auto p-3 space-y-2">
              {filteredNotifications.length === 0 ? (
                <div className="text-center py-12 text-muted-foreground">
                  <Bell size={32} className="mx-auto mb-3 opacity-30" />
                  <p className="text-sm font-medium">No notifications</p>
                </div>
              ) : (
                filteredNotifications.map(n => {
                  const Icon = icons[n.type]
                  return (
                    <div
                      key={n.id}
                      className={cn(
                        'p-3 rounded-xl border transition-all cursor-pointer',
                        n.read ? 'border-border bg-card' : 'border-primary/30 bg-primary/[0.02]',
                        !n.read && 'hover:bg-primary/[0.04]'
                      )}
                      onClick={() => markAsRead(n.id)}
                    >
                      <div className="flex items-start gap-3">
                        <div className={cn(
                          'p-2 rounded-lg flex-shrink-0',
                          n.severity === 'critical' ? 'bg-red-50 dark:bg-red-950/30' :
                          n.severity === 'warning' ? 'bg-amber-50 dark:bg-amber-950/30' :
                          'bg-blue-50 dark:bg-blue-950/30'
                        )}>
                          <Icon size={16} className={cn(
                            n.severity === 'critical' ? 'text-red-600' :
                            n.severity === 'warning' ? 'text-amber-600' :
                            'text-blue-600'
                          )} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <h4 className={cn(
                            'text-sm font-medium truncate',
                            !n.read && 'text-foreground'
                          )}>
                            {n.title}
                          </h4>
                          <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                            {n.message}
                          </p>
                          {n.agentId && (
                            <p className="text-xs font-mono text-muted-foreground mt-1.5">
                              Agent: {n.agentId}
                            </p>
                          )}
                          <div className="flex items-center gap-1.5 mt-2 text-xs text-muted-foreground">
                            <Clock size={12} />
                            <span>{new Date(n.createdAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}</span>
                          </div>
                        </div>
                        {!n.read && (
                          <div className="w-2 h-2 rounded-full bg-primary flex-shrink-0 mt-1.5" />
                        )}
                      </div>
                    </div>
                  )
                })
              )}
            </div>
          </>
        )}
      </div>
    </>
  )
}
