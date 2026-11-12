import { useState, useEffect } from 'react'
import { Link, useLocation, Outlet } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { navItems } from '@/components/layout/nav-items'
import { SeldonVigilLogo } from '@/components/SeldonVigilLogo'
import { MobileBottomNav } from '@/components/layout/MobileBottomNav'
import { MobileMenuDropdown } from '@/components/layout/MobileMenuDropdown'
import { useAuth } from '@/hooks/useAuth'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Search,
  Bell,
  Moon,
  Sun,
  LogOut,
  Menu,
  ChevronLeft,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useTheme } from '@/hooks/useTheme'
import { NotificationDrawer } from '@/components/ui/notification-drawer'
import { CommandPalette } from '@/components/ui/command-palette'
import { useNavigate } from 'react-router-dom'

export function CommandCenterLayout() {
  const location = useLocation()
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const [panelOpen, setPanelOpen] = useState(false)
  const [notificationOpen, setNotificationOpen] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)

  const isActive = (href: string) => location.pathname === href

  const displayName = user?.username || 'Guest'
  const initials = displayName
    .split(/[\s._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part[0]!.toUpperCase())
    .join('') || 'SU'
  const roleLabel = user?.roles?.length
    ? user.roles[0].replace(/[_-]+/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
    : 'Viewer'

  const commandItems = [
    { id: 'dashboard', label: 'Go to Dashboard', description: 'View your main dashboard', icon: <Menu size={16} />, shortcut: 'D', onSelect: () => navigate('/'), category: 'Navigation' },
    { id: 'traces', label: 'Go to Traces', description: 'View trace explorer', icon: <Menu size={16} />, shortcut: 'T', onSelect: () => navigate('/traces'), category: 'Navigation' },
    { id: 'security', label: 'Go to Security', description: 'View security violations', icon: <Menu size={16} />, shortcut: 'S', onSelect: () => navigate('/security'), category: 'Navigation' },
    { id: 'costs', label: 'Go to Costs', description: 'View cost intelligence', icon: <Menu size={16} />, shortcut: 'C', onSelect: () => navigate('/costs'), category: 'Navigation' },
    { id: 'settings', label: 'Go to Settings', description: 'View application settings', icon: <Menu size={16} />, shortcut: 'S', onSelect: () => navigate('/settings'), category: 'Navigation' },
  ]

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) { e.preventDefault(); setCommandOpen(true) }
    }
    document.addEventListener('keydown', down)
    return () => document.removeEventListener('keydown', down)
  }, [])

  const railContent = (
    <>
      {/* Logo */}
      <div className="flex items-center justify-center h-14 hairline-b">
        <motion.div
          className="w-9 h-9 rounded-xl bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center"
          whileHover={{ scale: 1.1, rotate: 5 }}
          transition={{ type: 'spring', stiffness: 300 }}
        >
          <SeldonVigilLogo size={22} className="text-white" />
        </motion.div>
      </div>

      {/* Nav icons */}
      <nav className="flex-1 py-4 space-y-1 overflow-y-auto">
        {navItems.map((item, index) => {
          const active = isActive(item.href)
          return (
            <Link
              key={item.href}
              to={item.href}
              className={cn(
                'relative flex items-center justify-center w-full h-11 transition-all duration-200',
                active ? 'text-primary' : 'text-sidebar-muted hover:text-sidebar-foreground'
              )}
              title={item.label}
              onMouseEnter={() => !active && setPanelOpen(true)}
            >
              {active && (
                <motion.div
                  layoutId="cc-active"
                  className="absolute left-0 w-[3px] h-6 bg-primary rounded-r-full"
                  transition={{ type: 'spring', stiffness: 300, damping: 30 }}
                />
              )}
              <motion.div
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: index * 0.03 }}
              >
                <item.icon size={20} />
              </motion.div>
              {item.badge && (
                <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-primary animate-pulse" />
              )}
            </Link>
          )
        })}
      </nav>

      {/* Bottom actions */}
      <div className="hairline-t py-3 space-y-1">
        <button
          onClick={toggleTheme}
          className="flex items-center justify-center w-full h-10 text-sidebar-muted hover:text-sidebar-foreground transition-colors"
          title="Toggle theme"
        >
          {theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
        </button>
        <button
          onClick={() => { logout(); navigate('/login') }}
          className="flex items-center justify-center w-full h-10 text-sidebar-muted hover:text-destructive transition-colors"
          title="Logout"
        >
          <LogOut size={18} />
        </button>
      </div>
    </>
  )

  const panelContent = (
    <>
      {/* Panel header */}
      <div className="flex items-center justify-between px-4 h-14 hairline-b">
        <span className="text-sm font-display font-semibold text-sidebar-foreground">Navigation</span>
        <button
          onClick={() => setPanelOpen(false)}
          className="p-1 rounded-lg text-sidebar-muted hover:text-sidebar-foreground hover:bg-emerald-500/[0.08] transition-all"
        >
          <ChevronLeft size={16} />
        </button>
      </div>

      {/* Nav labels */}
      <nav className="flex-1 px-2 py-4 space-y-0.5 overflow-y-auto">
        {navItems.map((item) => {
          const active = isActive(item.href)
          return (
            <Link
              key={item.href}
              to={item.href}
              className={cn(
                'flex items-center gap-3 px-3 py-2.5 rounded-xl text-[0.95rem] font-medium transition-all duration-200',
                active ? 'bg-primary/10 text-primary' : 'text-sidebar-muted hover:bg-emerald-500/[0.08] hover:text-sidebar-foreground'
              )}
            >
              <item.icon size={18} className="flex-shrink-0" />
              <span className="flex-1 truncate">{item.label}</span>
              {item.badge && (
                <span className="px-2 py-0.5 rounded-full bg-primary/20 text-primary text-[10px] font-semibold uppercase tracking-wider">
                  {item.badge}
                </span>
              )}
            </Link>
          )
        })}
      </nav>

      {/* User */}
      <div className="hairline-t p-3">
        <div className="flex items-center gap-3 rounded-xl px-3 py-2.5">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center text-white text-sm font-semibold flex-shrink-0 shadow-lg shadow-primary/25">
            {initials}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-[0.95rem] font-medium text-sidebar-foreground truncate">{displayName}</p>
            <p className="text-[0.82rem] text-sidebar-muted truncate">{roleLabel}</p>
          </div>
        </div>
      </div>
    </>
  )

  return (
    <div className="flex min-h-screen flex-col bg-background lg:h-screen lg:overflow-hidden">
      {/* Icon Rail */}
      <aside className="fixed left-0 top-0 h-screen w-[52px] bg-sidebar hairline-r flex-col z-50 hidden lg:flex">
        {railContent}
      </aside>

      {/* Secondary Panel — hover-triggered */}
      <div
        className="fixed left-[52px] top-0 h-screen z-40 hidden lg:flex"
        onMouseLeave={() => setPanelOpen(false)}
      >
        <AnimatePresence>
          {panelOpen && (
            <motion.div
              initial={{ width: 0, opacity: 0 }}
              animate={{ width: 220, opacity: 1 }}
              exit={{ width: 0, opacity: 0 }}
              transition={{ duration: 0.2, ease: 'easeInOut' }}
              className="h-full bg-sidebar hairline-r flex-col overflow-hidden"
            >
              {panelContent}
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Main content */}
      <div className="flex min-h-0 flex-1 flex-col lg:ml-[52px] lg:min-h-0">
        {/* Thin utility header */}
        <header className="sticky top-0 z-30 bg-background/80 backdrop-blur-xl hairline-b h-11">
          <div className="flex items-center justify-between px-4 h-full">
            <div className="flex items-center gap-2 flex-1">
              <MobileMenuDropdown buttonClassName="h-8 w-9" />
              <div className="relative flex-1 max-w-xs hidden sm:block">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  placeholder="Search... ⌘K"
                  className="pl-9 h-8 bg-muted/50 border-0 focus-visible:bg-background rounded-lg text-sm cursor-pointer"
                  onClick={() => setCommandOpen(true)}
                  readOnly
                />
              </div>
            </div>
            <div className="flex items-center gap-1">
              <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground" onClick={() => setNotificationOpen(true)}>
                <Bell size={16} />
              </Button>
              <div className="w-px h-4 bg-border mx-1" />
              <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center text-white text-xs font-semibold cursor-pointer">
                {user?.username?.charAt(0)?.toUpperCase() || 'U'}
              </div>
            </div>
          </div>
        </header>

        <main className="min-h-0 flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8 pb-28 lg:pb-8">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.5, ease: 'easeOut' }}
            className={location.pathname === '/docs' ? 'h-full' : ''}
          >
            <Outlet />
          </motion.div>
        </main>
      </div>

      <MobileBottomNav />
      <NotificationDrawer open={notificationOpen} onClose={() => setNotificationOpen(false)} />
      <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} items={commandItems} />
    </div>
  )
}
