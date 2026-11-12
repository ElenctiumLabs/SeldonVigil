import { useState, useEffect, useRef } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { navItems } from '@/components/layout/nav-items'
import { useAuth } from '@/hooks/useAuth'
import { motion, AnimatePresence } from 'framer-motion'
import { Menu, LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'

const DESKTOP_QUERY = '(min-width: 1024px)'

function useIsDesktop() {
  const [isDesktop, setIsDesktop] = useState(
    () => typeof window !== 'undefined' && window.matchMedia(DESKTOP_QUERY).matches
  )

  useEffect(() => {
    const mq = window.matchMedia(DESKTOP_QUERY)
    const onChange = (e: MediaQueryListEvent) => setIsDesktop(e.matches)
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }, [])

  return isDesktop
}

interface MobileMenuDropdownProps {
  className?: string
  buttonClassName?: string
  /** Keep the burger visible at desktop widths too (FloatingOverlay) */
  showOnDesktop?: boolean
}

/**
 * Shared burger menu used by all layouts below desktop width (< lg).
 * Opens a compact glass dropdown directly under the burger button.
 * Each layout's own full navigation takes over again at >= lg.
 */
export function MobileMenuDropdown({ className, buttonClassName, showOnDesktop }: MobileMenuDropdownProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const location = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const isDesktop = useIsDesktop()

  useEffect(() => { setOpen(false) }, [location.pathname])

  useEffect(() => {
    if (isDesktop && !showOnDesktop) setOpen(false)
  }, [isDesktop, showOnDesktop])

  // Close when clicking/tapping anywhere outside the menu. A fixed
  // full-screen click-catcher can't be used here because ancestor
  // `backdrop-blur` creates a containing block that confines it.
  useEffect(() => {
    if (!open) return
    const onOutside = (e: Event) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', onOutside)
    document.addEventListener('touchstart', onOutside)
    return () => {
      document.removeEventListener('mousedown', onOutside)
      document.removeEventListener('touchstart', onOutside)
    }
  }, [open])

  // Hide at desktop widths — must come AFTER all hooks so the hook
  // order stays stable when resizing across the breakpoint
  if (isDesktop && !showOnDesktop) return null

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

  return (
    <div ref={rootRef} className={cn('relative', className)}>
      <Button
        variant="ghost"
        size="icon"
        aria-label="Menu"
        className={cn('rounded-xl h-9 w-9 text-muted-foreground', buttonClassName)}
        onClick={() => setOpen(o => !o)}
      >
        <Menu size={20} />
      </Button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -8, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -8, scale: 0.98 }}
            transition={{ duration: 0.18, ease: 'easeOut' }}
            className="glass-card absolute top-[calc(100%+0.5rem)] left-0 w-60 z-50 overflow-hidden"
          >
              {/* User */}
              <div className="flex items-center gap-3 px-4 py-3 hairline-b">
                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-primary to-amber-600 flex items-center justify-center text-white text-xs font-semibold">
                  {initials}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-sidebar-foreground truncate">{displayName}</p>
                  <p className="text-[0.75rem] text-sidebar-muted truncate">{roleLabel}</p>
                </div>
              </div>

              {/* Nav */}
              <nav className="px-2 py-2 space-y-0.5 max-h-[60vh] overflow-y-auto">
                {navItems.map((item) => {
                  const active = location.pathname === item.href
                  return (
                    <Link
                      key={item.href}
                      to={item.href}
                      onClick={() => setOpen(false)}
                      className={cn(
                        'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200',
                        active ? 'bg-primary/10 text-primary' : 'text-sidebar-muted hover:bg-emerald-500/[0.08] hover:text-sidebar-foreground'
                      )}
                    >
                      <item.icon size={16} className="flex-shrink-0" />
                      <span className="flex-1 truncate">{item.label}</span>
                    </Link>
                  )
                })}
              </nav>

              {/* Footer */}
              <div className="hairline-t p-2">
                <button
                  onClick={() => { logout(); navigate('/login') }}
                  className="flex items-center gap-3 w-full px-3 py-2 rounded-lg text-sm font-medium text-sidebar-muted hover:bg-emerald-500/[0.08] hover:text-destructive transition-all"
                >
                  <LogOut size={16} />
                  <span>Sign out</span>
                </button>
              </div>
            </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
