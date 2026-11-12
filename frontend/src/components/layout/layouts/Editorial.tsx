import React, { useState, useEffect, useRef } from 'react'
import { Link, useLocation, Outlet } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { navItems } from '@/components/layout/nav-items'
import { SeldonVigilLogo } from '@/components/SeldonVigilLogo'
import { useAuth } from '@/hooks/useAuth'
import { motion, AnimatePresence } from 'framer-motion'
import { MobileBottomNav } from '@/components/layout/MobileBottomNav'
import { MobileMenuDropdown } from '@/components/layout/MobileMenuDropdown'
import { useDocsStore } from '@/lib/docsStore'
import {
  Search, Bell, Moon, Sun, LogOut, Menu, X, ChevronLeft, ChevronRight,
  LayoutDashboard, Activity, ShieldAlert, DollarSign, Scale,
  Link2, BrainCircuit, FileText, AlertTriangle, Clock,
  TrendingUp, Layers, Users, Zap, CheckCircle2, Plug, Target,
  Lightbulb, Sliders, BellRing, Wallet, Code,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useTheme } from '@/hooks/useTheme'
import { NotificationDrawer } from '@/components/ui/notification-drawer'
import { CommandPalette } from '@/components/ui/command-palette'
import { useNavigate } from 'react-router-dom'

interface SubNavItem {
  icon: React.ElementType
  label: string
  href?: string
}

// Items relegated to the bottom of the left sub-nav (standard placement
// for utility/config destinations); excluded from the top tab strip
const SECONDARY_NAV_HREFS = ['/broker', '/docs', '/settings']
const SECONDARY_NAV_ITEMS = navItems.filter(i => SECONDARY_NAV_HREFS.includes(i.href))
const PRIMARY_NAV_ITEMS = navItems.filter(i => !SECONDARY_NAV_HREFS.includes(i.href))

const subNavConfig: Record<string, SubNavItem[]> = {
  '/': [
    { icon: LayoutDashboard, label: 'Overview' },
    { icon: Activity,        label: 'Activity Feed' },
    { icon: TrendingUp,      label: 'Cost Summary' },
    { icon: CheckCircle2,    label: 'System Health' },
  ],
  '/traces': [
    { icon: Activity,   label: 'All Traces' },
    { icon: Clock,      label: 'Timeline' },
    { icon: Search,     label: 'Search' },
    { icon: AlertTriangle, label: 'Failed Traces' },
  ],
  '/security': [
    { icon: ShieldAlert,  label: 'Violations' },
    { icon: AlertTriangle, label: 'Threats' },
    { icon: FileText,     label: 'Audit Log' },
    { icon: Scale,        label: 'Policies' },
  ],
  '/costs': [
    { icon: DollarSign, label: 'Overview' },
    { icon: Users,      label: 'Agent Breakdown' },
    { icon: TrendingUp, label: 'Trends' },
    { icon: Wallet,     label: 'Budget' },
  ],
  '/broker': [
    { icon: Activity, label: 'Status' },
    { icon: Layers,   label: 'Topics' },
    { icon: Users,    label: 'Consumer Groups' },
    { icon: Zap,      label: 'Configuration' },
  ],
  '/governance': [
    { icon: Scale,      label: 'Policies' },
    { icon: CheckCircle2, label: 'Certification' },
    { icon: FileText,   label: 'Rules' },
    { icon: ShieldAlert, label: 'Compliance' },
  ],
  '/integrations': [
    { icon: Plug,     label: 'All Integrations' },
    { icon: CheckCircle2, label: 'Active' },
    { icon: X,        label: 'Inactive' },
    { icon: Link2,    label: 'Catalog' },
  ],
  '/ai-reasoning': [
    { icon: BrainCircuit, label: 'Analyses' },
    { icon: Target,       label: 'Triggers' },
    { icon: Clock,        label: 'History' },
    { icon: Lightbulb,    label: 'Recommendations' },
  ],
  '/settings': [
    { icon: Sliders,   label: 'Appearance' },
    { icon: BellRing,  label: 'Notifications' },
    { icon: Wallet,    label: 'Budget' },
    { icon: Code,      label: 'API' },
  ],
}

export function EditorialLayout() {
  const location = useLocation()
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const [notificationOpen, setNotificationOpen] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)
  const [subNavCollapsed, setSubNavCollapsed] = useState(false)
  // Docs accordion: only one section group expanded at a time
  const [openDocGroup, setOpenDocGroup] = useState<string | null>(null)

  // Live docs-viewer state — fed by postMessage via DocsPage
  const { groups: docGroups, activeId, activeSection } = useDocsStore()

  // Auto-expand the accordion group matching the currently active doc section
  useEffect(() => {
    if (location.pathname === '/docs') {
      if (activeSection) {
        setOpenDocGroup(activeSection)
      } else if (activeId) {
        const owner = docGroups.find((g) => g.sections.some((s) => s.id === activeId))
        if (owner) setOpenDocGroup(owner.key)
      }
    }
  }, [activeSection, activeId, docGroups, location.pathname])
  const scrollRef = useRef<HTMLDivElement>(null)
  const [showLeftArrow, setShowLeftArrow] = useState(false)
  const [showRightArrow, setShowRightArrow] = useState(true)

  const isActive = (href: string) => location.pathname === href
  const currentSubNav = subNavConfig[location.pathname] || subNavConfig['/']

  useEffect(() => {
    if (scrollRef.current) {
      const activeEl = scrollRef.current.querySelector('[data-active="true"]')
      if (activeEl) {
        activeEl.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' })
      }
    }
  }, [location.pathname])

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

  const checkScroll = () => {
    if (scrollRef.current) {
      const { scrollLeft, scrollWidth, clientWidth } = scrollRef.current
      setShowLeftArrow(scrollLeft > 10)
      setShowRightArrow(scrollLeft < scrollWidth - clientWidth - 10)
    }
  }

  useEffect(() => { checkScroll() }, [])

  const scrollTabs = (direction: 'left' | 'right') => {
    if (scrollRef.current) {
      scrollRef.current.scrollBy({ left: direction === 'left' ? -200 : 200, behavior: 'smooth' })
    }
  }

  const subNavWidth = subNavCollapsed ? 52 : 200

  return (
    <div className="h-screen bg-background flex flex-col overflow-hidden">
      {/* Header with horizontal nav */}
      <header className="sticky top-0 z-30 bg-background/80 backdrop-blur-xl hairline-b">
        {/* Top bar */}
        <div className="flex items-center justify-between px-4 lg:px-6 h-14">
          <div className="flex items-center gap-3">
            <MobileMenuDropdown />
            {/* HexagramMark + Cormorant SC wordmark — SeldonVigil */}
            <div className="flex items-center gap-2.5 group">
              <motion.div whileHover={{ scale: 1.08 }} transition={{ type: 'spring', stiffness: 320, damping: 22 }}>
                <SeldonVigilLogo size={30} />
              </motion.div>
              <span
                className="text-sidebar-foreground font-bold tracking-wider select-none hidden sm:block leading-none"
                style={{ fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif", fontSize: '1.1rem' }}
              >
                SELDONVIGIL
              </span>
            </div>
          </div>

          <div className="relative flex-1 max-w-md mx-4 hidden sm:block">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search or press Cmd+K..."
              className="pl-10 h-9 bg-muted/50 border-0 focus-visible:bg-background rounded-xl text-[0.95rem] cursor-pointer"
              onClick={() => setCommandOpen(true)}
              readOnly
            />
            <kbd className="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline-flex items-center gap-1 px-1.5 py-0.5 rounded-md bg-muted text-[10px] font-medium text-muted-foreground border border-border">⌘K</kbd>
          </div>

          <div className="flex items-center gap-1.5 lg:gap-2">
            <motion.div whileHover={{ scale: 1.1, rotate: 15 }} whileTap={{ scale: 0.95 }}>
              <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground" onClick={toggleTheme}>
                {theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
              </Button>
            </motion.div>
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
              <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground relative" onClick={() => setNotificationOpen(true)}>
                <Bell size={18} />
                <span className="absolute top-2 right-2 w-2 h-2 rounded-full bg-destructive ring-2 ring-background" />
              </Button>
            </motion.div>
            <div className="w-px h-6 bg-border mx-1 hidden sm:block" />
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
              <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground" onClick={() => { logout(); navigate('/login') }} title="Logout">
                <LogOut size={18} />
              </Button>
            </motion.div>
            <div className="w-px h-6 bg-border mx-1 hidden sm:block" />
            <motion.div className="flex items-center gap-3 pl-1 cursor-pointer" whileHover={{ scale: 1.02 }}>
              <div className="hidden sm:flex flex-col items-end">
                <p
                  className="text-foreground"
                  style={{ fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif", fontSize: '0.95rem', fontWeight: 600, letterSpacing: '0.04em' }}
                >
                  {user?.username || 'User'}
                </p>
                <p
                  className="text-muted-foreground"
                  style={{ fontFamily: "'Fira Code', monospace", fontSize: '0.68rem', letterSpacing: '0.06em', textTransform: 'uppercase', opacity: 0.6 }}
                >
                  {user?.roles?.[0] || 'viewer'}
                </p>
              </div>
              {/* Branded avatar — gold ring, Cormorant SC initial */}
              <motion.div
                style={{
                  width: 36, height: 36, borderRadius: '50%',
                  border: '1.5px solid rgba(251,191,36,0.55)',
                  background: 'rgba(251,191,36,0.08)',
                  boxShadow: '0 0 10px rgba(251,191,36,0.12)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  cursor: 'pointer', flexShrink: 0,
                }}
                whileHover={{ boxShadow: '0 0 22px rgba(251,191,36,0.30)', borderColor: 'rgba(251,191,36,0.85)' }}
                whileTap={{ scale: 0.95 }}
              >
                <span style={{ fontFamily: "'Cormorant SC', 'Cormorant Garamond', serif", fontSize: '1rem', fontWeight: 600, color: '#fbbf24', lineHeight: 1, letterSpacing: '0.04em' }}>
                  {user?.username?.charAt(0)?.toUpperCase() || 'U'}
                </span>
              </motion.div>
            </motion.div>
          </div>
        </div>

        {/* Horizontal tab navigation — desktop only; burger dropdown takes over below lg */}
        <div className="relative hairline-t hidden lg:block">
          {showLeftArrow && (
            <button
              onClick={() => scrollTabs('left')}
              className="absolute left-0 top-0 bottom-0 z-10 w-10 bg-gradient-to-r from-background to-transparent flex items-center justify-center lg:hidden"
            >
              <ChevronLeft size={16} className="text-muted-foreground" />
            </button>
          )}

          <div
            ref={scrollRef}
            onScroll={checkScroll}
            className="flex overflow-x-auto scrollbar-hide px-4 lg:px-6"
          >
            {PRIMARY_NAV_ITEMS.map((item) => {
              const active = isActive(item.href)
              return (
                <Link
                  key={item.href}
                  to={item.href}
                  data-active={active}
                  className={cn(
                    'relative flex items-center gap-1 px-4 py-2 text-base font-medium whitespace-nowrap transition-all duration-200 border-b-2 flex-shrink-0 rounded-t-lg',
                    active
                      ? 'text-primary border-primary'
                      : 'text-muted-foreground border-transparent hover:text-emerald-400 hover:border-emerald-500/40 hover:bg-emerald-500/[0.08]'
                  )}
                >
                  <item.icon size={16} />
                  <span>{item.label}</span>
                  {item.badge && (
                    <span className="px-1.5 py-0.5 rounded-full bg-primary/20 text-primary text-[9px] font-semibold uppercase tracking-wider">{item.badge}</span>
                  )}
                </Link>
              )
            })}
          </div>

          {showRightArrow && (
            <button
              onClick={() => scrollTabs('right')}
              className="absolute right-0 top-0 bottom-0 z-10 w-10 bg-gradient-to-l from-background to-transparent flex items-center justify-center lg:hidden"
            >
              <ChevronRight size={16} className="text-muted-foreground" />
            </button>
          )}
        </div>
      </header>

      {/* Body: sub-nav + content */}
      <div className="flex flex-1 overflow-hidden">
        {/* Sub-navigation panel */}
        <aside
          className={cn(
            'hidden lg:flex flex-col hairline-r bg-card/50 transition-all duration-300 flex-shrink-0 overflow-hidden'
          )}
          style={{ width: subNavWidth }}
        >
          {/* Sub-nav header */}
          <div className={cn(
            'flex items-center h-10 hairline-b',
            subNavCollapsed ? 'justify-center px-2' : 'justify-between px-4'
          )}>
            {!subNavCollapsed && (
              <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                {navItems.find(i => isActive(i.href))?.label || 'Menu'}
              </span>
            )}
            <button
              onClick={() => setSubNavCollapsed(!subNavCollapsed)}
              className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
            >
              {subNavCollapsed ? <ChevronRight size={14} /> : <ChevronLeft size={14} />}
            </button>
          </div>

          {/* Sub-nav items — docs accordion when on /docs, page sub-nav otherwise */}
          {location.pathname === '/docs' ? (
            <nav className="flex-1 py-2 overflow-y-auto scrollbar-themed">
              {subNavCollapsed ? (
                // Collapsed: a tick-mark per section group, hover highlights
                <div className="flex flex-col items-center gap-1 py-2">
                  {docGroups.map((g) => (
                    <div
                      key={g.key}
                      className={cn(
                        'w-6 h-px cursor-pointer transition-colors',
                        activeSection === g.key
                          ? 'bg-primary'
                          : 'bg-border hover:bg-primary/50',
                      )}
                      title={g.label}
                      onClick={() => navigate(`/docs?section=${g.key}`)}
                    />
                  ))}
                </div>
              ) : docGroups.length === 0 ? (
                <p className="px-4 py-3 text-xs text-muted-foreground">Loading docs…</p>
              ) : (
                docGroups.map((group) => {
                  const open = openDocGroup === group.key
                  const isGroupActive = activeSection === group.key
                  return (
                    <div key={group.key} className="mb-1">
                      <button
                        onClick={() => {
                          setOpenDocGroup(open ? null : group.key)
                          if (!open && group.sections.length > 0) {
                            navigate(`/docs?section=${group.sections[0].id}`)
                          }
                        }}
                        className={cn(
                          'w-full flex items-center justify-between px-4 pt-2 pb-1',
                          'text-[10px] font-semibold uppercase tracking-wider transition-colors',
                          isGroupActive
                            ? 'text-primary'
                            : 'text-muted-foreground/70 hover:text-muted-foreground',
                        )}
                      >
                        <span>{group.label}</span>
                        <ChevronRight
                          size={12}
                          className={cn(
                            'transition-transform duration-200 flex-shrink-0',
                            open && 'rotate-90',
                          )}
                        />
                      </button>

                      {open && group.sections && group.sections.length > 0 && (
                        <div className="py-0.5 space-y-0.5">
                          {group.sections.map((s) => {
                            const isSectionActive =
                              activeId === s.id ||
                              new URLSearchParams(location.search).get('section') === s.id
                            return (
                              <Link
                                key={s.id}
                                to={`/docs?section=${s.id}`}
                                className={cn(
                                  'block px-5 py-1.5 text-[13px] font-docs-nav truncate transition-colors duration-150',
                                  isSectionActive
                                    ? 'text-primary bg-primary/10 border-r-2 border-primary font-medium'
                                    : 'text-muted-foreground hover:text-foreground hover:bg-emerald-500/[0.08]'
                                )}
                              >
                                {s.title}
                              </Link>
                            )
                          })}
                        </div>
                      )}
                    </div>
                  )
                })
              )}
            </nav>
          ) : (
            <nav className="flex-1 py-2 overflow-y-auto scrollbar-themed">
              <AnimatePresence mode="wait">
                {currentSubNav.map((item, index) => {
                  const isFirst = index === 0
                  return (
                    <motion.button
                      key={item.label}
                      initial={{ opacity: 0, x: -8 }}
                      animate={{ opacity: 1, x: 0 }}
                      exit={{ opacity: 0, x: -8 }}
                      transition={{ delay: index * 0.03 }}
                      className={cn(
                        'w-full flex items-center gap-2.5 transition-all duration-200',
                        subNavCollapsed ? 'justify-center px-2 py-2.5' : 'px-4 py-2',
                        isFirst
                          ? 'text-primary bg-primary/5'
                           : 'text-muted-foreground hover:text-foreground hover:bg-emerald-500/[0.08]'
                      )}
                      title={subNavCollapsed ? item.label : undefined}
                    >
                      <item.icon size={15} className="flex-shrink-0" />
                      {!subNavCollapsed && (
                        <span className="text-[0.95rem] font-medium truncate">{item.label}</span>
                      )}
                      {!subNavCollapsed && isFirst && (
                        <div className="ml-auto w-1.5 h-1.5 rounded-full bg-primary" />
                      )}
                    </motion.button>
                  )
                })}
              </AnimatePresence>
            </nav>
          )}

          {/* Secondary nav + utilities — pinned to the bottom of the sub-nav */}
          <div className="hairline-t p-2 space-y-0.5">
            {SECONDARY_NAV_ITEMS.map((item) => {
              const active = isActive(item.href) || (item.href !== '/settings' && location.pathname.startsWith(item.href))
              return (
                <Link
                  key={item.href}
                  to={item.href}
                  className={cn(
                    'flex items-center gap-2.5 rounded-lg text-sm font-medium transition-all duration-200',
                    subNavCollapsed ? 'justify-center px-2 py-2' : 'px-3 py-2',
                    active ? 'text-primary bg-primary/5' : 'text-muted-foreground hover:text-foreground hover:bg-emerald-500/[0.08]'
                  )}
                  title={subNavCollapsed ? item.label : undefined}
                >
                  <item.icon size={15} className="flex-shrink-0" />
                  {!subNavCollapsed && <span className="truncate">{item.label}</span>}
                </Link>
              )
            })}
            <div className={cn(
              'pt-1 hairline-t flex items-center gap-1',
              subNavCollapsed ? 'flex-col justify-center mt-1' : 'justify-between px-1'
            )}>
              <button
                onClick={toggleTheme}
                title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
                className="p-2 rounded-lg text-muted-foreground hover:text-emerald-400 hover:bg-emerald-500/[0.08] transition-colors"
              >
                {theme === 'dark' ? <Moon size={16} /> : <Sun size={16} />}
              </button>
              <button
                onClick={() => { logout(); navigate('/login') }}
                title="Logout"
                className="p-2 rounded-lg text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors"
              >
                <LogOut size={16} />
              </button>
            </div>
          </div>
        </aside>

        {/* Main content */}
        <main className={cn('flex-1 scrollbar-themed', location.pathname === '/docs' ? 'overflow-hidden' : 'overflow-y-auto')}>
          <div
            className={cn(
              'w-full px-4 lg:pl-4 lg:pr-8 py-6 pb-28 lg:pb-8',
              location.pathname === '/docs' && 'h-full'
            )}
          >
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.5, ease: 'easeOut' }}
              className={location.pathname === '/docs' ? 'h-full' : ''}
            >
              <Outlet />
            </motion.div>
          </div>
        </main>
      </div>

      <MobileBottomNav />
      <NotificationDrawer open={notificationOpen} onClose={() => setNotificationOpen(false)} />
      <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} items={commandItems} />
    </div>
  )
}
