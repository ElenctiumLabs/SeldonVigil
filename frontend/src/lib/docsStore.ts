/**
 * Minimal shared store for the docs-viewer integration.
 *
 * DocsPage populates it via postMessage from the iframe; EditorialLayout reads
 * it to render the docs-nav accordion with section links.
 */
import { useState, useEffect } from 'react'

export interface DocSection {
  id: string
  title: string
  slug: string
  section: string
}

export interface DocGroup {
  key: string
  label: string
  order: number
  sections: DocSection[]
}

/**
 * Mirror of SECTION_META from tools/docs-viewer/src/main.tsx.
 * Keep in sync when new sections are added to docs/.
 */
export const SECTION_META: Record<string, { label: string; order: number }> = {
  overview:     { label: 'Overview',            order: 1  },
  stack:        { label: 'Tech Stack',          order: 2  },
  services:     { label: 'Backend Services',    order: 3  },
  modules:      { label: 'Modules',             order: 4  },
  api:          { label: 'API Reference',       order: 5  },
  frontend:     { label: 'Frontend',            order: 6  },
  data:         { label: 'Data & Storage',      order: 7  },
  deployment:   { label: 'Deployment',          order: 8  },
  decisions:    { label: 'Decisions & Roadmap', order: 9  },
  architecture: { label: 'Architecture',        order: 20 },
  runbooks:     { label: 'Runbooks',            order: 21 },
  development:  { label: 'Development',         order: 22 },
  compliance:   { label: 'Compliance',          order: 23 },
  security:     { label: 'Security',            order: 24 },
}

type Listener = () => void

let _groups: DocGroup[] = []
let _activeId: string | null = null
let _activeSection: string | null = null
const _listeners = new Set<Listener>()

function notify() {
  _listeners.forEach((l) => l())
}

export function setDocsGroups(groups: DocGroup[]) {
  _groups = groups
  notify()
}

export function setDocsActive(id: string | null, section: string | null) {
  _activeId = id
  _activeSection = section
  notify()
}

export function useDocsStore() {
  const [, tick] = useState(0)
  useEffect(() => {
    const l = () => tick((c) => c + 1)
    _listeners.add(l)
    return () => {
      _listeners.delete(l)
    }
  }, [])
  return { groups: _groups, activeId: _activeId, activeSection: _activeSection }
}
