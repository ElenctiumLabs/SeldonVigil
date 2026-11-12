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
  Search, Bell, Moon, Sun, LogOut, Menu, ChevronLeft, ChevronRight,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useTheme } from '@/hooks/useTheme'
import { NotificationDrawer } from '@/components/ui/notification-drawer'
import { CommandPalette } from '@/components/ui/command-palette'
import { useNavigate } from 'react-router-dom'

export function DefaultLayout() {
  const location = useLocation()
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
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

  const sidebarContent = (
    <>
      {/* Logo Section */}
      <div className="flex items-center justify-between px-5 h-16 hairline-b">
        <div className="flex items-center gap-2.5 group">
          {/* HexagramMark — SeldonVigil brand mark */}
          <motion.div
            whileHover={{ scale: 1.08 }}
            transition={{ type: 'spring', stiffness: 320, damping: 22 }}
            style={{ flexShrink: 0 }}
          >
            <SeldonVigilLogo size={30} />
          </motion.div>
          <AnimatePresence mode="wait">
            {!sidebarCollapsed && (
              <motion.span
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -10 }}
                transition={{ duration: 0.18 }}
                className="text-foreground font-bold tracking-wider select-none leading-none"
                style={{
                  fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif",
                  fontSize: '1.15rem',
                }}
              >
                SELDONVIGIL
              </motion.span>
            )}
          </AnimatePresence>
        </div>
        <motion.button
          onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
          className="hidden lg:flex p-1.5 rounded-lg text-sidebar-muted hover:text-sidebar-foreground hover:bg-white/5 transition-all duration-200"
          whileHover={{ scale: 1.1 }}
          whileTap={{ scale: 0.95 }}
        >
          {sidebarCollapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
        </motion.button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-6 space-y-1 overflow-y-auto">
        {navItems.map((item, index) => (
          <Link
            key={item.href}
            to={item.href}
            className={cn(
              'sidebar-link group relative',
              isActive(item.href) ? 'sidebar-link-active' : 'sidebar-link-inactive',
              sidebarCollapsed && 'justify-center px-0'
            )}
            title={sidebarCollapsed ? item.label : undefined}
          >
            {isActive(item.href) && (
              <motion.div
                layoutId="default-active"
                className="absolute inset-0 bg-primary/10 rounded-xl -z-10"
                transition={{ type: 'spring', stiffness: 300, damping: 30 }}
              />
            )}
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: index * 0.05 }}
              className="flex items-center"
            >
              <item.icon size={20} className="flex-shrink-0" />
              <AnimatePresence mode="wait">
                {!sidebarCollapsed && (
                  <motion.div
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, x: -10 }}
                    transition={{ duration: 0.2 }}
                    className="flex items-center flex-1"
                  >
                    <span className="flex-1 truncate">{item.label}</span>
                    {item.badge && (
                      <motion.span
                        className="px-2 py-0.5 rounded-full bg-primary/20 text-primary text-[10px] font-semibold uppercase tracking-wider"
                        animate={{
                          boxShadow: isActive(item.href) ? '0 0 10px rgba(251, 191, 36, 0.3)' : 'none'
                        }}
                      >
                        {item.badge}
                      </motion.span>
                    )}
                  </motion.div>
                )}
              </AnimatePresence>
            </motion.div>
          </Link>
        ))}
      </nav>

      {/* User Profile Section */}
      <div className={cn(
        "hairline-t p-3",
        sidebarCollapsed && "flex flex-col items-center"
      )}>
        <motion.div
          className={cn(
            "flex items-center gap-3 rounded-xl px-3 py-2.5",
            !sidebarCollapsed && "hover:bg-white/5 transition-colors cursor-pointer"
          )}
          whileHover={{ scale: 1.02 }}
          whileTap={{ scale: 0.98 }}
        >
          {/* Branded avatar: gold ring + Cormorant SC initials */}
          <div
            className="flex-shrink-0"
            style={{
              width: 36,
              height: 36,
              borderRadius: '50%',
              border: '1.5px solid rgba(251,191,36,0.55)',
              background: 'rgba(251,191,36,0.08)',
              boxShadow: '0 0 10px rgba(251,191,36,0.12)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <span
              style={{
                fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif",
                fontSize: '0.95rem',
                fontWeight: 600,
                color: '#fbbf24',
                lineHeight: 1,
                letterSpacing: '0.04em',
              }}
            >
              {initials}
            </span>
          </div>
          <AnimatePresence mode="wait">
            {!sidebarCollapsed && (
              <motion.div
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -10 }}
                transition={{ duration: 0.18 }}
                className="flex-1 min-w-0"
              >
                <p
                  className="truncate text-sidebar-foreground"
                  style={{
                    fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif",
                    fontSize: '0.95rem',
                    fontWeight: 600,
                    letterSpacing: '0.04em',
                  }}
                >
                  {displayName}
                </p>
                <p
                  className="truncate text-muted-foreground"
                  style={{
                    fontFamily: "'Fira Code', monospace",
                    fontSize: '0.68rem',
                    letterSpacing: '0.06em',
                    textTransform: 'uppercase',
                    opacity: 0.65,
                  }}
                >
                  {roleLabel}
                </p>
              </motion.div>
            )}
          </AnimatePresence>
        </motion.div>
      </div>
    </>
  )

  return (
    <div className="flex min-h-screen flex-col bg-background lg:h-screen lg:overflow-hidden">
      {/* Desktop Sidebar */}
      <aside className={cn(
        'fixed left-0 top-0 h-screen bg-sidebar hairline-r flex-col z-50 transition-all duration-300 hidden lg:flex',
        sidebarCollapsed ? 'w-[72px]' : 'w-64'
      )}>
        {sidebarContent}
      </aside>

      {/* Mobile Sidebar Drawer */}
      {/* Main content */}
      <motion.div
        className={cn(
          'flex min-h-0 flex-1 flex-col transition-all duration-300 min-h-screen lg:min-h-0',
          sidebarCollapsed ? 'lg:ml-[72px]' : 'lg:ml-64'
        )}
        layout
      >
        {/* Header */}
        <motion.header
          className="sticky top-0 z-30 bg-background/80 backdrop-blur-xl hairline-b"
          initial={{ y: -20, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          transition={{ duration: 0.3 }}
        >
          <div className="flex items-center justify-between px-4 lg:px-6 h-16">
            <div className="flex items-center gap-3 flex-1">
              <MobileMenuDropdown />
              <div className="relative flex-1 max-w-md hidden sm:block">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <motion.div whileHover={{ scale: 1.02 }} whileTap={{ scale: 0.98 }}>
                  <Input
                    placeholder="Search or press Cmd+K..."
                    className="pl-10 h-9 bg-muted/50 border-0 focus-visible:bg-background rounded-xl text-[0.95rem] cursor-pointer glass-card"
                    onClick={() => setCommandOpen(true)}
                    readOnly
                  />
                </motion.div>
                <kbd className="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline-flex items-center gap-1 px-1.5 py-0.5 rounded-md bg-muted text-[10px] font-medium text-muted-foreground border border-border">
                  <span className="text-xs">⌘</span>K
                </kbd>
              </div>
            </div>

            <div className="flex items-center gap-1.5 lg:gap-2">
              <motion.div whileHover={{ scale: 1.1, rotate: 15 }} whileTap={{ scale: 0.95 }}>
                <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground" onClick={toggleTheme} title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}>
                  <AnimatePresence mode="wait">
                    <motion.div key={theme} initial={{ rotate: -90, opacity: 0 }} animate={{ rotate: 0, opacity: 1 }} exit={{ rotate: 90, opacity: 0 }} transition={{ duration: 0.2 }}>
                      {theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
                    </motion.div>
                  </AnimatePresence>
                </Button>
              </motion.div>
              <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground relative" onClick={() => setNotificationOpen(true)}>
                  <Bell size={18} />
                  <motion.span className="absolute top-2 right-2 w-2 h-2 rounded-full bg-destructive ring-2 ring-background" animate={{ scale: [1, 1.2, 1] }} transition={{ duration: 2, repeat: Infinity }} />
                </Button>
              </motion.div>
              <div className="w-px h-6 bg-border mx-1 hidden sm:block" />
              <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
                <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground" onClick={() => { logout(); navigate('/login') }} title="Logout">
                  <LogOut size={18} />
                </Button>
              </motion.div>
              <div className="w-px h-6 bg-border mx-1 hidden sm:block" />
              <motion.div className="flex items-center gap-3 pl-1" whileHover={{ scale: 1.02 }}>
                <div className="hidden sm:flex flex-col items-end">
                  <p
                    className="text-foreground"
                    style={{
                      fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif",
                      fontSize: '0.95rem',
                      fontWeight: 600,
                      letterSpacing: '0.04em',
                    }}
                  >
                    {user?.username || 'User'}
                  </p>
                  <p
                    className="text-muted-foreground"
                    style={{
                      fontFamily: "'Fira Code', monospace",
                      fontSize: '0.68rem',
                      letterSpacing: '0.06em',
                      textTransform: 'uppercase',
                      opacity: 0.6,
                    }}
                  >
                    {user?.roles?.[0] || 'viewer'}
                  </p>
                </div>
                {/* Branded avatar circle — gold ring, Cormorant SC initial */}
                <motion.div
                  style={{
                    width: 36,
                    height: 36,
                    borderRadius: '50%',
                    border: '1.5px solid rgba(251,191,36,0.55)',
                    background: 'rgba(251,191,36,0.08)',
                    boxShadow: '0 0 10px rgba(251,191,36,0.12)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    cursor: 'pointer',
                    flexShrink: 0,
                  }}
                  whileHover={{ boxShadow: '0 0 22px rgba(251,191,36,0.30)', borderColor: 'rgba(251,191,36,0.85)' }}
                  whileTap={{ scale: 0.95 }}
                >
                  <span
                    style={{
                      fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif",
                      fontSize: '1rem',
                      fontWeight: 600,
                      color: '#fbbf24',
                      lineHeight: 1,
                      letterSpacing: '0.04em',
                    }}
                  >
                    {user?.username?.charAt(0)?.toUpperCase() || 'U'}
                  </span>
                </motion.div>
              </motion.div>
            </div>
          </div>
        </motion.header>

        <main className="min-h-0 flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8 pb-28 lg:pb-8">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.5 }}
            className={location.pathname === '/docs' ? 'h-full' : ''}
          >
            <Outlet />
          </motion.div>
        </main>
      </motion.div>

      <MobileBottomNav />
      <NotificationDrawer open={notificationOpen} onClose={() => setNotificationOpen(false)} />
      <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} items={commandItems} />
    </div>
  )
}
