import { Search, Bell, Moon, Sun, Menu, LayoutDashboard, Activity, Shield, DollarSign, Users, Settings, LogOut } from 'lucide-react'
import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useTheme } from '@/hooks/useTheme'
import { useAuth } from '@/hooks/useAuth'
import { NotificationDrawer } from '@/components/ui/notification-drawer'
import { CommandPalette } from '@/components/ui/command-palette'
import { useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'

interface HeaderProps {
  onMobileMenuToggle?: () => void
}

const COMMAND_ITEMS = [
  {
    id: 'dashboard',
    label: 'Go to Dashboard',
    description: 'View your main dashboard',
    icon: <LayoutDashboard size={16} />,
    shortcut: 'D',
    onSelect: () => {},
    category: 'Navigation',
  },
  {
    id: 'agents',
    label: 'Go to Agents',
    description: 'View agent inventory and activate agents',
    icon: <Users size={16} />,
    shortcut: 'A',
    onSelect: () => {},
    category: 'Navigation',
  },
  {
    id: 'traces',
    label: 'Go to Traces',
    description: 'View trace explorer',
    icon: <Activity size={16} />,
    shortcut: 'T',
    onSelect: () => {},
    category: 'Navigation',
  },
  {
    id: 'security',
    label: 'Go to Security',
    description: 'View security violations',
    icon: <Shield size={16} />,
    shortcut: 'S',
    onSelect: () => {},
    category: 'Navigation',
  },
  {
    id: 'costs',
    label: 'Go to Costs',
    description: 'View cost intelligence',
    icon: <DollarSign size={16} />,
    shortcut: 'C',
    onSelect: () => {},
    category: 'Navigation',
  },
  {
    id: 'settings',
    label: 'Go to Settings',
    description: 'View application settings',
    icon: <Settings size={16} />,
    shortcut: 'S',
    onSelect: () => {},
    category: 'Navigation',
  },
]

export function Header({ onMobileMenuToggle }: HeaderProps) {
  const { theme, toggleTheme } = useTheme()
  const { user, logout } = useAuth()
  const [notificationOpen, setNotificationOpen] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)
  const navigate = useNavigate()

  // Add navigation to command items
  const commandItems = COMMAND_ITEMS.map(item => ({
    ...item,
    onSelect: () => {
      const paths: Record<string, string> = {
        dashboard: '/',
        traces: '/traces',
        security: '/security',
        costs: '/costs',
        settings: '/settings',
      }
      navigate(paths[item.id] || '/')
    },
  }))

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault()
        setCommandOpen(true)
      }
    }
    document.addEventListener('keydown', down)
    return () => document.removeEventListener('keydown', down)
  }, [])

  return (
    <>
      <motion.header 
        className="sticky top-0 z-30 bg-background/80 backdrop-blur-xl hairline-b"
        initial={{ y: -20, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={{ duration: 0.3 }}
      >
        <div className="flex items-center justify-between px-4 lg:px-6 h-header">
          {/* Left: Mobile Menu + Search */}
          <div className="flex items-center gap-3 flex-1">
            {/* Mobile Menu Toggle */}
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
              <Button
                variant="ghost"
                size="icon"
                className="lg:hidden rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground flex-shrink-0"
                onClick={onMobileMenuToggle}
              >
                <Menu size={20} />
              </Button>
            </motion.div>

            {/* Search / Command Trigger */}
            <div className="relative flex-1 max-w-md hidden sm:block">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <motion.div
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
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

          {/* Right: Actions */}
          <div className="flex items-center gap-1.5 lg:gap-2">
            {/* Theme Toggle */}
            <motion.div whileHover={{ scale: 1.1, rotate: 15 }} whileTap={{ scale: 0.95 }}>
              <Button
                variant="ghost"
                size="icon"
                className="rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground"
                onClick={toggleTheme}
                title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
              >
                <AnimatePresence mode="wait">
                  <motion.div
                    key={theme}
                    initial={{ rotate: -90, opacity: 0 }}
                    animate={{ rotate: 0, opacity: 1 }}
                    exit={{ rotate: 90, opacity: 0 }}
                    transition={{ duration: 0.2 }}
                  >
                    {theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
                  </motion.div>
                </AnimatePresence>
              </Button>
            </motion.div>

            {/* Notifications */}
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
              <Button
                variant="ghost"
                size="icon"
                className="rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground relative"
                onClick={() => setNotificationOpen(true)}
              >
                <Bell size={18} />
                <motion.span 
                  className="absolute top-2 right-2 w-2 h-2 rounded-full bg-destructive ring-2 ring-background"
                  animate={{ scale: [1, 1.2, 1] }}
                  transition={{ duration: 2, repeat: Infinity }}
                />
              </Button>
            </motion.div>

            {/* Divider */}
            <div className="w-px h-6 bg-border mx-1 hidden sm:block" />

            {/* Logout */}
            <motion.div whileHover={{ scale: 1.1 }} whileTap={{ scale: 0.95 }}>
              <Button
                variant="ghost"
                size="icon"
                className="rounded-xl h-9 w-9 text-muted-foreground hover:text-foreground"
                onClick={() => {
                  logout()
                  navigate('/login')
                }}
                title="Logout"
              >
                <LogOut size={18} />
              </Button>
            </motion.div>

            {/* Divider */}
            <div className="w-px h-6 bg-border mx-1 hidden sm:block" />

            {/* User Avatar */}
            <motion.div 
              className="flex items-center gap-3 pl-1"
              whileHover={{ scale: 1.02 }}
            >
              <div className="hidden sm:flex flex-col items-end">
                <p className="text-[0.95rem] font-medium text-foreground">{user?.username || 'User'}</p>
                <p className="text-[0.82rem] text-muted-foreground">{user?.roles?.join(', ') || 'User'}</p>
              </div>
              <motion.div 
                className="w-9 h-9 rounded-xl bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center text-white text-sm font-semibold cursor-pointer shadow-lg shadow-primary/25 flex-shrink-0"
                whileHover={{ scale: 1.1, boxShadow: '0 0 20px rgba(251, 191, 36, 0.4)' }}
                whileTap={{ scale: 0.95 }}
              >
                {user?.username?.charAt(0)?.toUpperCase() || 'U'}
              </motion.div>
            </motion.div>
          </div>
        </div>
      </motion.header>

      {/* Notification Drawer */}
      <NotificationDrawer
        open={notificationOpen}
        onClose={() => setNotificationOpen(false)}
      />

      {/* Command Palette */}
      <CommandPalette
        open={commandOpen}
        onClose={() => setCommandOpen(false)}
        items={commandItems}
      />
    </>
  )
}
