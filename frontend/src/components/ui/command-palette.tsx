import { useState, useEffect, type ReactNode, useMemo } from 'react'
import { Search, Command } from 'lucide-react'

interface CommandItem {
  id: string
  label: string
  description?: string
  icon?: ReactNode
  shortcut?: string
  onSelect: () => void
  category?: string
}

interface CommandPaletteProps {
  open: boolean
  onClose: () => void
  items: CommandItem[]
}

export function CommandPalette({ open, onClose, items }: CommandPaletteProps) {
  const [search, setSearch] = useState('')

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault()
        if (!open) {
          // Open would be handled by parent
        }
      }
      if (e.key === 'Escape') {
        onClose()
      }
    }
    document.addEventListener('keydown', down)
    return () => document.removeEventListener('keydown', down)
  }, [open, onClose])

  useEffect(() => {
    if (open) {
      document.body.style.overflow = 'hidden'
    }
    return () => {
      document.body.style.overflow = ''
    }
  }, [open])

  const filteredItems = useMemo(() => {
    if (!search) return items
    const term = search.toLowerCase()
    return items.filter(item =>
      item.label.toLowerCase().includes(term) ||
      item.description?.toLowerCase().includes(term) ||
      item.category?.toLowerCase().includes(term)
    )
  }, [search, items])

  if (!open) return null

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50 z-50 animate-fade-in"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-full max-w-lg z-50 animate-scale-in">
        <div className="bg-background rounded-2xl border border-border shadow-xl overflow-hidden">
          {/* Search */}
          <div className="flex items-center gap-3 p-4 border-b border-border">
            <Search size={18} className="text-muted-foreground" />
            <input
              type="text"
              placeholder="Search commands..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
              autoFocus
            />
            <div className="flex items-center gap-1 text-xs text-muted-foreground">
              <Command size={12} />
              <span>K</span>
            </div>
          </div>

          {/* Results */}
          <div className="max-h-96 overflow-y-auto p-2">
            {filteredItems.length === 0 ? (
              <div className="p-6 text-center text-muted-foreground">
                <p className="text-sm">No results found</p>
              </div>
            ) : (
              <div className="space-y-1">
                {filteredItems.map(item => (
                  <button
                    key={item.id}
                    onClick={() => {
                      item.onSelect()
                      onClose()
                    }}
                    className="w-full flex items-center gap-3 p-3 rounded-xl hover:bg-muted/50 transition-colors text-left"
                  >
                    {item.icon && <div className="text-muted-foreground">{item.icon}</div>}
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-foreground">{item.label}</p>
                      {item.description && (
                        <p className="text-xs text-muted-foreground mt-0.5">{item.description}</p>
                      )}
                    </div>
                    {item.shortcut && (
                      <div className="flex items-center gap-1 text-xs text-muted-foreground">
                        {item.shortcut.split('').map((char, i) => (
                          <span key={i} className="px-1 py-0.5 rounded bg-muted border border-border">
                            {char}
                          </span>
                        ))}
                      </div>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  )
}
