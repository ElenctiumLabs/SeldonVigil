import { StrictMode, useEffect, useMemo, useRef, useState } from 'react'
import { createRoot } from 'react-dom/client'
import ReactMarkdown, { type Components } from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { DOCS } from 'virtual:docs'
import './globals.css'

type RawDoc = { slug: string; section: string; title: string; markdown: string }

export interface DocItem extends RawDoc {
  id: string
}

/** Sidebar section labels and order for the docs tree. */
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

const rawDocs = DOCS as RawDoc[]

/**
 * Maps markdown slug to the classic anchor IDs used by SeldonVigil Reference
 * e.g. overview/what-is-seldonvigil -> overview
 */
function slugToId(slug: string): string {
  const mapping: Record<string, string> = {
    'reference/overview/what-is-seldonvigil': 'overview',
    'overview/what-is-seldonvigil': 'overview',
    'reference/overview/architecture': 'architecture',
    'overview/architecture': 'architecture',
    'reference/overview/core-concepts': 'concepts',
    'overview/core-concepts': 'concepts',
    'reference/stack/backend': 'stack-backend',
    'stack/backend': 'stack-backend',
    'reference/stack/frontend': 'stack-frontend',
    'stack/frontend': 'stack-frontend',
    'reference/stack/infrastructure': 'stack-infra',
    'stack/infrastructure': 'stack-infra',
    'reference/services/api': 'service-api',
    'services/api': 'service-api',
    'reference/services/workers': 'service-workers',
    'services/workers': 'service-workers',
    'reference/services/sdk': 'service-sdk',
    'services/sdk': 'service-sdk',
    'reference/services/cli': 'service-cli',
    'services/cli': 'service-cli',
    'reference/services/shared-kernel': 'service-sharedkernel',
    'services/shared-kernel': 'service-sharedkernel',
    'reference/modules/identity': 'mod-identity',
    'modules/identity': 'mod-identity',
    'reference/modules/observability': 'mod-observability',
    'modules/observability': 'mod-observability',
    'reference/modules/cost-intelligence': 'mod-costintelligence',
    'modules/cost-intelligence': 'mod-costintelligence',
    'reference/modules/security': 'mod-security',
    'modules/security': 'mod-security',
    'reference/modules/governance': 'mod-governance',
    'modules/governance': 'mod-governance',
    'reference/modules/ai-reasoning': 'mod-aireasoning',
    'modules/ai-reasoning': 'mod-aireasoning',
    'reference/modules/analytics': 'mod-analytics',
    'modules/analytics': 'mod-analytics',
    'reference/modules/integration': 'mod-integration',
    'modules/integration': 'mod-integration',
    'reference/modules/messaging': 'mod-messaging',
    'modules/messaging': 'mod-messaging',
    'reference/api/auth': 'api-auth',
    'api/auth': 'api-auth',
    'reference/api/agents': 'api-agents',
    'api/agents': 'api-agents',
    'reference/api/traces': 'api-traces',
    'api/traces': 'api-traces',
    'reference/api/costs': 'api-costs',
    'api/costs': 'api-costs',
    'reference/api/security': 'api-security',
    'api/security': 'api-security',
    'reference/api/governance': 'api-governance',
    'api/governance': 'api-governance',
    'reference/api/broker': 'api-broker',
    'api/broker': 'api-broker',
    'reference/api/ai-reasoning': 'api-ai',
    'api/ai-reasoning': 'api-ai',
    'reference/api/integrations': 'api-integrations',
    'api/integrations': 'api-integrations',
    'reference/api/system': 'api-system',
    'api/system': 'api-system',
    'reference/frontend/pages': 'frontend-pages',
    'frontend/pages': 'frontend-pages',
    'reference/frontend/hooks': 'frontend-hooks',
    'frontend/hooks': 'frontend-hooks',
    'reference/data/postgresql': 'data-postgres',
    'data/postgresql': 'data-postgres',
    'reference/data/broker-topics': 'data-broker',
    'data/broker-topics': 'data-broker',
    'reference/data/cache': 'data-cache',
    'data/cache': 'data-cache',
    'reference/data/vector-store': 'data-vectors',
    'data/vector-store': 'data-vectors',
    'reference/deployment/docker-compose': 'deploy-docker',
    'deployment/docker-compose': 'deploy-docker',
    'reference/deployment/ci-cd': 'deploy-cicd',
    'deployment/ci-cd': 'deploy-cicd',
    'reference/decisions/architecture-decisions': 'adr',
    'decisions/architecture-decisions': 'adr',
    'reference/decisions/roadmap': 'roadmap',
    'decisions/roadmap': 'roadmap',
    'reference/decisions/known-gaps': 'gaps',
    'decisions/known-gaps': 'gaps',
  }
  if (mapping[slug]) return mapping[slug]
  return slug.replace(/[^a-zA-Z0-9_-]+/g, '-')
}

function prettyName(slug: string) {
  return slug
    .split('/')
    .pop()!
    .replace(/[-_]+/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
}

const docsWithId: DocItem[] = rawDocs.map((d) => ({
  ...d,
  id: slugToId(d.slug),
}))

/** Mermaid is loaded on demand so the initial bundle stays small. */
let mermaidPromise: Promise<typeof import('mermaid').default> | null = null

function getMermaid() {
  if (!mermaidPromise) {
    mermaidPromise = import('mermaid').then((m) => {
      m.default.initialize({
        startOnLoad: false,
        securityLevel: 'loose',
        theme: 'dark',
        themeVariables: {
          primaryColor: '#161b22',
          primaryTextColor: '#f0f6fc',
          primaryBorderColor: '#30363d',
          lineColor: '#fbbf24',
          secondaryColor: '#0d1117',
          tertiaryColor: '#1c2128',
          fontFamily: 'Inter, system-ui, sans-serif',
          fontSize: '13px',
        },
      })
      return m.default
    })
  }
  return mermaidPromise
}

/** Renders a fenced ```mermaid block; falls back to the source on error. */
function Mermaid({ chart }: { chart: string }) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!ref.current) return
    const id = `mmd-${Math.random().toString(36).slice(2)}`
    let cancelled = false
    getMermaid()
      .then((mermaid) => mermaid.render(id, chart))
      .then(({ svg }) => {
        if (!cancelled && ref.current) ref.current.innerHTML = svg
      })
      .catch(() => {
        if (!cancelled && ref.current) {
          ref.current.innerHTML = `<pre class="mermaid-fallback">${chart}</pre>`
        }
      })
    return () => {
      cancelled = true
    }
  }, [chart])

  return <div ref={ref} className="mermaid-diagram-wrap" />
}

const markdownComponents: Components = {
  code({ node, className, children, ...props }) {
    const match = /language-(\w+)/.exec(className || '')
    if (match?.[1] === 'mermaid') {
      return <Mermaid chart={String(children).replace(/\n$/, '')} />
    }
    return (
      <code className={className} {...props}>
        {children}
      </code>
    )
  },
}

function App() {
  const [query, setQuery] = useState('')
  const [activeId, setActiveId] = useState<string>('overview')
  const [isEmbedded, setIsEmbedded] = useState(false)
  const [theme, setTheme] = useState<'light' | 'dark'>(() =>
    document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark',
  )

  // Detect whether we are embedded in an iframe inside the main SeldonVigil app
  useEffect(() => {
    try {
      setIsEmbedded(window.self !== window.top)
    } catch {
      setIsEmbedded(true)
    }
  }, [])

  const toggleTheme = () => {
    const next = theme === 'light' ? 'dark' : 'light'
    setTheme(next)
    document.documentElement.setAttribute('data-theme', next)
    try {
      localStorage.setItem('seldonvigil-theme', next)
    } catch {
      /* ignore */
    }
  }

  // Group documentation files by section metadata order
  const sections = useMemo(() => {
    const by = new Map<string, DocItem[]>()
    for (const d of docsWithId) {
      if (!by.has(d.section)) by.set(d.section, [])
      by.get(d.section)!.push(d)
    }
    return [...by.entries()]
      .map(([key, list]) => ({
        key,
        label: SECTION_META[key]?.label ?? key,
        order: SECTION_META[key]?.order ?? 50,
        docs: list.sort((a, b) => a.title.localeCompare(b.title)),
      }))
      .sort((a, b) => a.order - b.order || a.label.localeCompare(b.label))
  }, [])

  // Filter sections by search query
  const filteredSections = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return sections
    return sections
      .map((s) => ({
        ...s,
        docs: s.docs.filter(
          (d) =>
            d.title.toLowerCase().includes(q) ||
            d.slug.toLowerCase().includes(q) ||
            d.id.toLowerCase().includes(q) ||
            d.markdown.toLowerCase().includes(q),
        ),
      }))
      .filter((s) => s.docs.length > 0)
  }, [query, sections])

  // Flat list of all visible docs
  const allVisibleDocs = useMemo(() => {
    return filteredSections.flatMap((s) => s.docs)
  }, [filteredSections])

  // One document at a time: the active document is the one shown in the pane
  const activeDoc = allVisibleDocs.find((d) => d.id === activeId) ?? allVisibleDocs[0]

  // Show a single document and tell the host shell which one is active
  const selectDoc = (id: string) => {
    const doc = docsWithId.find((d) => d.id === id)
    if (!doc) return
    setActiveId(doc.id)
    window.parent.postMessage(
      { type: 'active', id: doc.id, section: doc.section, slug: doc.slug },
      '*',
    )
  }

  // ── Shell Integration (postMessage) ──────────────────────────────────────────
  useEffect(() => {
    // Send ready event and complete TOC structure so host app can populate its accordion
    window.parent.postMessage({ type: 'ready' }, '*')
    window.parent.postMessage(
      {
        type: 'toc',
        groups: sections.map((sec) => ({
          key: sec.key,
          label: sec.label,
          order: sec.order,
          sections: sec.docs.map((d) => ({
            id: d.id,
            title: d.title || prettyName(d.slug),
            slug: d.slug,
            section: d.section,
          })),
        })),
        docs: docsWithId.map((d) => ({
          id: d.id,
          slug: d.slug,
          section: d.section,
          title: d.title || prettyName(d.slug),
        })),
      },
      '*',
    )
  }, [sections])

  // Each document starts at the top of the content pane
  useEffect(() => {
    const pane = document.getElementById('content')
    if (pane) pane.scrollTop = 0
    window.scrollTo(0, 0)
  }, [activeDoc?.id])

  // Listen for navigation & theme commands from the parent SeldonVigil shell
  useEffect(() => {
    const handler = (e: MessageEvent) => {
      const data = e.data ?? {}
      if (data.type === 'theme' && (data.value === 'light' || data.value === 'dark')) {
        document.documentElement.setAttribute('data-theme', data.value)
        setTheme(data.value as 'light' | 'dark')
      } else if (data.type === 'navigate' && data.id) {
        selectDoc(data.id)
      } else if (data.type === 'navigate-slug' && data.slug) {
        const doc = docsWithId.find((d) => d.slug === data.slug)
        if (doc) selectDoc(doc.id)
      } else if (data.type === 'navigate-section' && data.section) {
        // Find the first document belonging to this section
        const first = docsWithId.find((d) => d.section === data.section || d.id === data.section)
        if (first) selectDoc(first.id)
      }
    }
    window.addEventListener('message', handler)
    return () => window.removeEventListener('message', handler)
  }, [])

  return (
    <div className={`viewer ${isEmbedded ? 'embedded' : ''}`}>
      {/* Sidebar for standalone mode — suppressed via CSS when embedded in Editorial layout */}
      <aside id="sidebar" className="sidebar">
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <div className="sidebar-logo">◈</div>
            <div className="sidebar-title">
              <h1>SeldonVigil</h1>
              <p>Reference Docs</p>
            </div>
          </div>
          <button
            onClick={toggleTheme}
            className="theme-toggle-btn"
            title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
          >
            {theme === 'light' ? '☀' : '☾'}
          </button>
        </div>

        <div className="sidebar-search-wrap">
          <input
            className="search-input"
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search docs…"
          />
        </div>

        <nav className="sidebar-nav">
          {filteredSections.map((section) => (
            <div key={section.key} className="nav-group">
              <p className="nav-section-label">{section.label}</p>
              <ul>
                {section.docs.map((d) => (
                  <li key={d.id}>
                    <button
                      onClick={() => selectDoc(d.id)}
                      className={`nav-link-btn ${activeId === d.id ? 'active' : ''}`}
                    >
                      {d.title || prettyName(d.slug)}
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>
      </aside>

      {/* Main Content Pane — one document at a time */}
      <main id="content" className={`content-pane ${isEmbedded ? 'full-width' : ''}`}>
        <div className="doc-article-container">
          {activeDoc ? (
            <section
              key={activeDoc.id}
              id={activeDoc.id}
              className="doc-section"
              data-section={activeDoc.section}
              data-slug={activeDoc.slug}
            >
              <span className="section-eyebrow">
                {SECTION_META[activeDoc.section]?.label ?? activeDoc.section}
              </span>
              <ReactMarkdown remarkPlugins={[remarkGfm]} components={markdownComponents}>
                {activeDoc.markdown}
              </ReactMarkdown>
            </section>
          ) : (
            <div style={{ padding: '60px 0', textAlign: 'center', color: 'var(--text-muted)' }}>
              <p>No documentation found matching “{query}”.</p>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
