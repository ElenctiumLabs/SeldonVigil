import { createContext, useContext, useState, type ReactNode, useEffect } from 'react'

interface ThemeCustomizerState {
  primaryColor: string
  radius: 'default' | 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl'
}

interface ThemeCustomizerContextType {
  state: ThemeCustomizerState
  updateState: (newState: Partial<ThemeCustomizerState>) => void
  reset: () => void
}

const ThemeCustomizerContext = createContext<ThemeCustomizerContextType | undefined>(undefined)

const STORAGE_KEY = 'seldonvigil-theme-customizer'

const defaultState: ThemeCustomizerState = {
  primaryColor: '248 90% 66%', // Purple
  radius: 'default',
}

function getInitialState(): ThemeCustomizerState {
  if (typeof window === 'undefined') return defaultState
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored) {
    try {
      return JSON.parse(stored)
    } catch {
      return defaultState
    }
  }
  return defaultState
}

export function ThemeCustomizerProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<ThemeCustomizerState>(getInitialState)

  useEffect(() => {
    const root = document.documentElement
    root.style.setProperty('--primary', state.primaryColor)
    root.style.setProperty('--radius', getRadiusValue(state.radius))
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state))
  }, [state])

  const updateState = (newState: Partial<ThemeCustomizerState>) => {
    setState(prev => ({ ...prev, ...newState }))
  }

  const reset = () => {
    setState(defaultState)
  }

  return (
    <ThemeCustomizerContext.Provider value={{ state, updateState, reset }}>
      {children}
    </ThemeCustomizerContext.Provider>
  )
}

function getRadiusValue(radius: string): string {
  const values: Record<string, string> = {
    default: '0.5rem',
    sm: '0.25rem',
    md: '0.5rem',
    lg: '0.75rem',
    xl: '1rem',
    '2xl': '1.25rem',
    '3xl': '1.5rem',
  }
  return values[radius] || values.default
}

export function useThemeCustomizer() {
  const context = useContext(ThemeCustomizerContext)
  if (!context) {
    throw new Error('useThemeCustomizer must be used within ThemeCustomizerProvider')
  }
  return context
}
