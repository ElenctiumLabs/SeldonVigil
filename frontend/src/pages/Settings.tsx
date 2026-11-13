import { useState } from 'react'
import { Switch } from '@/components/ui/switch'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { PageLoader } from '@/components/ui/page-loader'
import { Settings as SettingsIcon, Bell, DollarSign, Globe, LayoutGrid } from 'lucide-react'
import { useSettings } from '@/hooks/useApi'
import { useLayout, LAYOUT_OPTIONS, type LayoutId } from '@/hooks/useLayout'
import { FadeIn } from '@/components/ui/animations'
import { cn } from '@/lib/utils'
import {
  Command,
  LayoutDashboard,
  Maximize2,
} from 'lucide-react'

const LAYOUT_ICONS: Record<LayoutId, React.ReactNode> = {
  'default': <LayoutDashboard size={20} />,
  'command-center': <Command size={20} />,
  'floating-overlay': <Maximize2 size={20} />,
  'editorial': <LayoutGrid size={20} />,
}

// Marks a control that is displayed but not yet wired up. Without this, an inert
// input reads as a working one and the page promises something it does not do.
function ComingSoon() {
  return (
    <span className="ml-2 rounded-full border border-border bg-muted px-2 py-0.5 text-[10px] font-mono uppercase tracking-wider text-muted-foreground">
      Coming soon
    </span>
  )
}

export function SettingsPage() {
  const { data: settings, isLoading, error } = useSettings()
  const { layout, setLayout } = useLayout()
  const [theme, setTheme] = useState<'light' | 'dark' | 'system'>(settings?.theme || 'system')
  const [notifications, setNotifications] = useState<{
    email: boolean
    slack: boolean
    securityAlerts: boolean
    costAlerts: boolean
  }>({
    email: settings?.notifications.email ?? false,
    slack: settings?.notifications.slack ?? false,
    securityAlerts: settings?.notifications.securityAlerts ?? true,
    costAlerts: settings?.notifications.costAlerts ?? true,
  })
  const [budgetAlerts, setBudgetAlerts] = useState<boolean>(settings?.budget.alertsEnabled ?? true)

  if (isLoading) {
    return <PageLoader label="Loading settings" />
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <div>
            <h1 className="page-title font-display text-4xl gradient-text">Settings</h1>
            <p className="page-subtitle">Configure application preferences and integrations</p>
          </div>
        </div>
        <div className="text-center py-12">
          <p className="text-destructive">Failed to load settings</p>
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
            <h1 className="page-title font-display text-4xl gradient-text">Settings</h1>
            <p className="page-subtitle">
              Configure application preferences and integrations
            </p>
          </div>
        </div>
      </FadeIn>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Layout Picker */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="stat-icon-container flex-shrink-0">
                <LayoutGrid size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
              <CardTitle className="font-display">Layout</CardTitle>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground mb-4">Choose how SeldonVigil's navigation and content areas are arranged.</p>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
              {LAYOUT_OPTIONS.map((option) => (
                <button
                  key={option.id}
                  onClick={() => setLayout(option.id)}
                  className={cn(
                    'relative flex flex-col items-center gap-3 p-4 rounded-xl border-2 transition-all duration-200 text-center group',
                    layout === option.id
                      ? 'border-primary bg-primary/5 shadow-sm shadow-primary/10'
                      : 'border-border hover:border-primary/30 hover:bg-muted/30'
                  )}
                >
                  {layout === option.id && (
                    <div className="absolute top-2 right-2 w-2 h-2 rounded-full bg-primary" />
                  )}
                  <div className={cn(
                    'p-2.5 rounded-xl transition-colors',
                    layout === option.id
                      ? 'bg-primary/10 text-primary'
                      : 'bg-muted text-muted-foreground group-hover:text-foreground'
                  )}>
                    {LAYOUT_ICONS[option.id]}
                  </div>
                  <div>
                    <p className={cn(
                      'text-sm font-medium transition-colors',
                      layout === option.id ? 'text-primary' : 'text-foreground'
                    )}>{option.label}</p>
                    <p className="text-[11px] text-muted-foreground mt-0.5 leading-tight">{option.description}</p>
                  </div>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Appearance Settings */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="stat-icon-container flex-shrink-0">
                <Globe size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
              <CardTitle className="font-display flex items-center">Appearance <ComingSoon /></CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="theme">Theme</Label>
              <Select
                value={theme}
                onValueChange={(val) => setTheme(val as 'light' | 'dark' | 'system')}
                disabled
                options={[
                  { value: 'light', label: 'Light' },
                  { value: 'dark', label: 'Dark' },
                  { value: 'system', label: 'System' },
                ]}
                placeholder="Select theme"
              />
            </div>
          </CardContent>
        </Card>

        {/* Notification Settings */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="stat-icon-container flex-shrink-0">
                <Bell size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
              <CardTitle className="font-display flex items-center">Notifications <ComingSoon /></CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="email-notifications">Email Notifications</Label>
                <p className="text-xs text-muted-foreground">Receive alerts via email</p>
              </div>
              <Switch 
                id="email-notifications" 
                disabled
                checked={notifications.email}
                onCheckedChange={(checked) => setNotifications(prev => ({ ...prev, email: checked as boolean }))}
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="slack-notifications">Slack Notifications</Label>
                <p className="text-xs text-muted-foreground">Send alerts to Slack</p>
              </div>
              <Switch 
                id="slack-notifications" 
                disabled
                checked={notifications.slack}
                onCheckedChange={(checked) => setNotifications(prev => ({ ...prev, slack: checked }))}
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="security-alerts">Security Alerts</Label>
                <p className="text-xs text-muted-foreground">Get notified of security events</p>
              </div>
              <Switch 
                id="security-alerts" 
                disabled
                checked={notifications.securityAlerts}
                onCheckedChange={(checked) => setNotifications(prev => ({ ...prev, securityAlerts: checked }))}
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="cost-alerts">Cost Alerts</Label>
                <p className="text-xs text-muted-foreground">Get notified of budget changes</p>
              </div>
              <Switch 
                id="cost-alerts" 
                disabled
                checked={notifications.costAlerts}
                onCheckedChange={(checked) => setNotifications(prev => ({ ...prev, costAlerts: checked }))}
              />
            </div>
          </CardContent>
        </Card>

        {/* Budget Settings */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="stat-icon-container flex-shrink-0">
                <DollarSign size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
              <CardTitle className="font-display flex items-center">Budget & Costs <ComingSoon /></CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="monthly-limit">Monthly Budget Limit (£)</Label>
              <Input
                id="monthly-limit"
                disabled
                type="number"
                defaultValue={settings?.budget.monthlyLimit || 5000}
                placeholder="5000"
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="budget-alerts">Budget Alerts</Label>
                <p className="text-xs text-muted-foreground">Alert when approaching budget limit</p>
              </div>
              <Switch 
                id="budget-alerts" 
                disabled
                checked={budgetAlerts}
                onCheckedChange={setBudgetAlerts}
              />
            </div>
          </CardContent>
        </Card>

        {/* API Settings */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="stat-icon-container flex-shrink-0">
                <SettingsIcon size={22} strokeWidth={1.5} className="text-emerald-400" />
              </div>
              <CardTitle className="font-display flex items-center">API Configuration <ComingSoon /></CardTitle>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="polling-interval">Polling Interval (ms)</Label>
              <Input
                id="polling-interval"
                disabled
                type="number"
                defaultValue={settings?.api.pollingInterval || 30000}
                placeholder="30000"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="timeout">Request Timeout (ms)</Label>
              <Input
                id="timeout"
                disabled
                type="number"
                defaultValue={settings?.api.timeout || 10000}
                placeholder="10000"
              />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Read-only note. These settings come from the server; editing is not wired up yet. */}
      <div className="flex justify-end">
        <p className="text-sm text-muted-foreground">
          Settings are read from the server. Editing is coming soon; layout is stored in your browser.
        </p>
      </div>
    </div>
  )
}
