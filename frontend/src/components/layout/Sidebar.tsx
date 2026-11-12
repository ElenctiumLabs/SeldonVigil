import { Link, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'
import {
  LayoutDashboard,
  Activity,
  ShieldAlert,
  DollarSign,
  Database,
  Settings,
  ChevronLeft,
  ChevronRight,
  X,
  LucideIcon,
  Scale,
  Link2,
  BrainCircuit
} from 'lucide-react'
import { useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useAuth } from '@/hooks/useAuth'
import { SeldonVigilLogo } from '@/components/SeldonVigilLogo'

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
  mobileOpen: boolean
  onMobileClose: () => void
}

interface NavItem {
  icon: LucideIcon
  label: string
  href: string
  badge?: string
}

const navItems: NavItem[] = [
  { icon: LayoutDashboard, label: 'Dashboard',        href: '/' },
  { icon: Activity,        label: 'Traces',           href: '/traces',   badge: 'Live' },
  { icon: ShieldAlert,     label: 'Security',         href: '/security' },
  { icon: DollarSign,      label: 'Cost Intelligence', href: '/costs' },
  { icon: Database,        label: 'Broker',            href: '/broker' },
  { icon: Scale,           label: 'Governance',       href: '/governance' },
  { icon: Link2,           label: 'Integrations',     href: '/integrations' },
  { icon: BrainCircuit,    label: 'AI Reasoning',     href: '/ai-reasoning' },
  { icon: Settings,        label: 'Settings',         href: '/settings' },
]

export function Sidebar({ collapsed, onToggle, mobileOpen, onMobileClose }: SidebarProps) {
  const location = useLocation()
  const { user } = useAuth()
  const isActive = (href: string) => location.pathname === href

  // Derive profile display values from the authenticated user, with
  // sensible fallbacks for missing data.
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

  // Close mobile sidebar on route change
  useEffect(() => {
    onMobileClose()
  }, [location.pathname, onMobileClose])

  // Prevent body scroll when mobile sidebar is open
  useEffect(() => {
    if (mobileOpen) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = ''
    }
    return () => {
      document.body.style.overflow = ''
    }
  }, [mobileOpen])

  const sidebarContent = (
    <>
      {/* Logo Section */}
      <div className="flex items-center justify-between px-5 h-16 hairline-b">
        <div className="flex items-center gap-3">
          <motion.div
            className="w-8 h-8 rounded-xl bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center flex-shrink-0"
            whileHover={{ scale: 1.1, rotate: 5 }}
            transition={{ type: "spring", stiffness: 300 }}
          >
            <SeldonVigilLogo size={24} className="text-white" />
          </motion.div>
          <AnimatePresence mode="wait">
            {!collapsed && (
              <motion.span
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -10 }}
                transition={{ duration: 0.2 }}
                className="text-sidebar-foreground font-display font-bold text-lg tracking-tight"
              >
                SeldonVigil
              </motion.span>
            )}
          </AnimatePresence>
        </div>
        {/* Desktop toggle */}
        <motion.button
          onClick={onToggle}
          className="hidden lg:flex p-1.5 rounded-lg text-sidebar-muted hover:text-sidebar-foreground hover:bg-white/5 transition-all duration-200"
          whileHover={{ scale: 1.1 }}
          whileTap={{ scale: 0.95 }}
        >
          {collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
        </motion.button>
        {/* Mobile close */}
        <motion.button
          onClick={onMobileClose}
          className="lg:hidden p-1.5 rounded-lg text-sidebar-muted hover:text-sidebar-foreground hover:bg-white/5 transition-all duration-200"
          whileHover={{ scale: 1.1 }}
          whileTap={{ scale: 0.95 }}
        >
          <X size={18} />
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
              collapsed && 'justify-center px-0'
            )}
            title={collapsed ? item.label : undefined}
          >
            {isActive(item.href) && (
              <motion.div
                layoutId="activeIndicator"
                className="absolute inset-0 bg-primary/10 rounded-xl -z-10"
                transition={{ type: "spring", stiffness: 300, damping: 30 }}
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
                {!collapsed && (
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
        collapsed && "flex flex-col items-center"
      )}>
        <motion.div
          className={cn(
            "flex items-center gap-3 rounded-xl px-3 py-2.5",
            !collapsed && "hover:bg-white/5 transition-colors cursor-pointer"
          )}
          whileHover={{ scale: 1.02 }}
          whileTap={{ scale: 0.98 }}
        >
          <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center text-white text-sm font-semibold flex-shrink-0 shadow-lg shadow-primary/25">
            {initials}
          </div>
          <AnimatePresence mode="wait">
            {!collapsed && (
              <motion.div
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -10 }}
                transition={{ duration: 0.2 }}
                className="flex-1 min-w-0"
              >
                <p className="text-[0.95rem] font-medium text-sidebar-foreground truncate">
                  {displayName}
                </p>
                <p className="text-[0.82rem] text-sidebar-muted truncate">
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
    <>
      {/* Desktop Sidebar */}
      <aside
        className={cn(
          'fixed left-0 top-0 h-screen bg-sidebar hairline-r flex-col z-50 transition-all duration-300 hidden lg:flex',
          collapsed ? 'w-[72px]' : 'w-64'
        )}
      >
        {sidebarContent}
      </aside>

      {/* Mobile Sidebar Overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 lg:hidden animate-fade-in"
          onClick={onMobileClose}
        />
      )}

      {/* Mobile Sidebar Drawer */}
      <aside
        className={cn(
          'fixed left-0 top-0 h-screen bg-sidebar hairline-r flex-col z-50 transition-all duration-300 flex lg:hidden',
          mobileOpen ? 'translate-x-0' : '-translate-x-full'
        )}
        style={{ width: '280px' }}
      >
        {sidebarContent}
      </aside>
    </>
  )
}
