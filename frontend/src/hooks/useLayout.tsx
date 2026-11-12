import { createContext, useContext, useState, type ReactNode, useEffect } from 'react'

export type LayoutId = 'default' | 'command-center' | 'floating-overlay' | 'editorial'

export interface LayoutOption {
  id: LayoutId
  label: string
  description: string
}

export const LAYOUT_OPTIONS: LayoutOption[] = [
  { id: 'default',           label: 'Default',           description: 'Classic sidebar + header shell' },
  { id: 'command-center',    label: 'Command Center',    description: 'Icon rail with collapsible secondary panel' },
  { id: 'floating-overlay',  label: 'Floating Overlay',  description: 'Minimal chrome, floating glass header' },
  { id: 'editorial',         label: 'Editorial',         description: 'Horizontal tabs, hero section, asymmetric blocks' },
]

interface LayoutContextType {
  layout: LayoutId
  setLayout: (id: LayoutId) => void
  layouts: LayoutOption[]
}

const LayoutContext = createContext<LayoutContextType | undefined>(undefined)

const STORAGE_KEY = 'seldonvigil-layout'

function getInitialLayout(): LayoutId {
  if (typeof window === 'undefined') return 'default'
  const stored = localStorage.getItem(STORAGE_KEY) as LayoutId | null
  if (stored && LAYOUT_OPTIONS.some(l => l.id === stored)) return stored
  return 'default'
}

export function LayoutProvider({ children }: { children: ReactNode }) {
  const [layout, setLayoutState] = useState<LayoutId>(getInitialLayout)

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, layout)
  }, [layout])

  const setLayout = (id: LayoutId) => {
    setLayoutState(id)
  }

  return (
    <LayoutContext.Provider value={{ layout, setLayout, layouts: LAYOUT_OPTIONS }}>
      {children}
    </LayoutContext.Provider>
  )
}

export function useLayout(): LayoutContextType {
  const context = useContext(LayoutContext)
  if (!context) {
    throw new Error('useLayout must be used within a LayoutProvider')
  }
  return context
}
