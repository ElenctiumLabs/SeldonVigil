import { Link, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { navItems } from '@/components/layout/nav-items'

/**
 * Icon-only sticky bottom navigation for screens smaller than desktop
 * (< lg). Mirrors the FloatingOverlay bottom bar; hidden at desktop
 * widths where each layout's full navigation takes over.
 */
export function MobileBottomNav() {
  const location = useLocation()
  const isActive = (href: string) => location.pathname === href

  return (
    <nav className="lg:hidden fixed bottom-0 left-1/2 -translate-x-1/2 z-40 w-[calc(100%-2rem)] max-w-5xl">
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
  )
}
