import { useState, useEffect } from 'react'
import { Link, useLocation, Outlet } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { navItems } from '@/components/layout/nav-items'
import { SeldonVigilLogo } from '@/components/SeldonVigilLogo'
import { MobileMenuDropdown } from '@/components/layout/MobileMenuDropdown'
import { useAuth } from '@/hooks/useAuth'
import { motion } from 'framer-motion'
import {
  Search, Bell, Moon, Sun, LogOut, Menu,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useTheme } from '@/hooks/useTheme'
import { NotificationDrawer } from '@/components/ui/notification-drawer'
import { CommandPalette } from '@/components/ui/command-palette'
import { useNavigate } from 'react-router-dom'

export function FloatingOverlayLayout() {
  const location = useLocation()
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const [notificationOpen, setNotificationOpen] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)

  const isActive = (href: string) => location.pathname === href

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) { e.preventDefault(); setCommandOpen(true) }
    }
    document.addEventListener('keydown', down)
    return () => document.removeEventListener('keydown', down)
  }, [])

  const commandItems = [
    { id: 'dashboard', label: 'Go to Dashboard', description: 'View your main dashboard', icon: <Menu size={16} />, shortcut: 'D', onSelect: () => navigate('/'), category: 'Navigation' },
    { id: 'traces', label: 'Go to Traces', description: 'View trace explorer', icon: <Menu size={16} />, shortcut: 'T', onSelect: () => navigate('/traces'), category: 'Navigation' },
    { id: 'security', label: 'Go to Security', description: 'View security violations', icon: <Menu size={16} />, shortcut: 'S', onSelect: () => navigate('/security'), category: 'Navigation' },
    { id: 'costs', label: 'Go to Costs', description: 'View cost intelligence', icon: <Menu size={16} />, shortcut: 'C', onSelect: () => navigate('/costs'), category: 'Navigation' },
    { id: 'settings', label: 'Go to Settings', description: 'View application settings', icon: <Menu size={16} />, shortcut: 'S', onSelect: () => navigate('/settings'), category: 'Navigation' },
  ]

  return (
    <div className="flex min-h-screen flex-col bg-background lg:h-screen lg:overflow-hidden">
      {/* Floating header */}
      <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 w-[calc(100%-2rem)] max-w-5xl">
        <motion.header
          className="glass-panel px-4 h-14 flex items-center justify-between shadow-lg shadow-black/20"
          initial={{ y: -20, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          transition={{ duration: 0.4, ease: 'easeOut' }}
        >
          {/* Left */}
          <div className="flex items-center gap-3">
            <MobileMenuDropdown showOnDesktop />
            <div className="flex items-center gap-2">
              <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center">
                <SeldonVigilLogo size={16} className="text-white" />
              </div>
              <span className="text-sm font-display font-bold text-foreground hidden sm:block">SeldonVigil</span>
            </div>
          </div>

          {/* Center — search */}
          <div className="relative flex-1 max-w-sm mx-4 hidden sm:block">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search... ⌘K"
              className="pl-10 h-9 bg-muted/30 border-0 focus-visible:bg-background rounded-xl text-[0.95rem] cursor-pointer"
              onClick={() => setCommandOpen(true)}
              readOnly
            />
          </div>

          {/* Right */}
          <div className="flex items-center gap-1.5">
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
            <div className="w-px h-5 bg-border mx-0.5 hidden sm:block" />
            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
              <Button variant="ghost" size="icon" className="rounded-xl h-9 w-9 text-muted-foreground" onClick={() => { logout(); navigate('/login') }} title="Logout">
                <LogOut size={18} />
              </Button>
            </motion.div>
            <div className="w-px h-5 bg-border mx-0.5 hidden sm:block" />
            <motion.div className="flex items-center gap-2 cursor-pointer" whileHover={{ scale: 1.02 }}>
              <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center text-white text-xs font-semibold shadow-md shadow-primary/20">
                {user?.username?.charAt(0)?.toUpperCase() || 'U'}
              </div>
            </motion.div>
          </div>
        </motion.header>
      </div>

      {/* Content — centered column matching the floating menu width */}
      <main className="min-h-0 flex-1 overflow-y-auto pt-24 px-4 sm:px-6 lg:px-8 pb-28 w-full max-w-5xl mx-auto">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.5, ease: 'easeOut' }}
          className={location.pathname === '/docs' ? 'h-full' : ''}
        >
          <Outlet />
        </motion.div>
      </main>

      {/* Sticky bottom nav — icon-only */}
      <nav className="fixed bottom-0 left-1/2 -translate-x-1/2 z-40 w-[calc(100%-2rem)] max-w-5xl">
        <div
          className="glass-panel mb-3 px-3 h-14 flex items-center justify-around shadow-lg shadow-black/20"
          style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
        >
          {navItems.map((item) => {
            const active = isActive(item.href)
            return (
              <Link
                key={item.href}
                to={item.href}
                title={item.label}
                className={cn(
                  'relative flex items-center justify-center w-11 h-11 rounded-xl transition-all duration-200',
                  active ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:text-foreground hover:bg-emerald-500/[0.08]'
                )}
              >
                <item.icon size={20} />
                {item.badge && (
                  <span className="absolute top-1 right-1 px-1 min-w-[16px] h-4 rounded-full bg-primary text-primary-foreground text-[9px] font-semibold flex items-center justify-center">
                    {item.badge}
                  </span>
                )}
              </Link>
            )
          })}
        </div>
      </nav>

      <NotificationDrawer open={notificationOpen} onClose={() => setNotificationOpen(false)} />
      <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} items={commandItems} />
    </div>
  )
}
