import { cn } from '@/lib/utils'

interface PageLoaderProps {
  label?: string
  className?: string
}

/**
 * Full-page deferred loading state. Rendered instead of page content
 * while data loads, so the layout doesn't stretch or jump; the loaded
 * content then animates in via its own fade-in.
 */
export function PageLoader({ label = 'Loading', className }: PageLoaderProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center gap-4 min-h-[50vh]', className)} role="status" aria-live="polite">
      <div className="relative w-10 h-10">
        <div className="absolute inset-0 rounded-full border-2 border-border" />
        <div className="absolute inset-0 rounded-full border-2 border-transparent border-t-primary animate-spin" />
        <div className="absolute inset-0 rounded-full bg-primary/10 blur-md animate-pulse-glow opacity-40" />
      </div>
      <p className="text-sm text-muted-foreground tracking-wide">{label}…</p>
    </div>
  )
}
