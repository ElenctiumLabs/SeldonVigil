/**
 * Hosts the SeldonVigil documentation viewer as an integrated single-page reference.
 *
 * In the Editorial layout, the viewer's native sidebar is hidden via injected CSS;
 * the shell's sub-nav accordion acts as the sidebar, and clicking sections
 * scrolls smoothly through the single continuous documentation page.
 * The app's light/dark theme is automatically bridged into the iframe.
 */
import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useLayout } from '@/hooks/useLayout'
import { useTheme } from '@/hooks/useTheme'
import { cn } from '@/lib/utils'
import { setDocsGroups, setDocsActive, type DocGroup } from '@/lib/docsStore'

export function DocsPage() {
  const { layout } = useLayout()
  const { theme } = useTheme()
  const [searchParams, setSearchParams] = useSearchParams()
  const section = searchParams.get('section')
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const [iframeReady, setIframeReady] = useState(false)
  const hideChrome = layout === 'editorial'

  const themeRef = useRef(theme)
  const sectionRef = useRef(section)
  // Last section the viewer reported as active while scrolling. Used to avoid
  // echoing a scroll-driven ?section= change back as a 'navigate' command,
  // which would re-scroll, re-fire the scroll-spy and loop.
  const lastActiveRef = useRef<string | null>(null)
  useEffect(() => { themeRef.current = theme }, [theme])
  useEffect(() => { sectionRef.current = section }, [section])

  const post = (msg: object) => {
    iframeRef.current?.contentWindow?.postMessage(msg, '*')
  }

  // Inject shell-specific styling into iframe on load
  const syncIframe = () => {
    const iframe = iframeRef.current
    const idoc = iframe?.contentDocument
    if (!iframe || !idoc) return

    if (hideChrome) {
      const existing = idoc.getElementById('shell-style')
      if (!existing) {
        const style = idoc.createElement('style')
        style.id = 'shell-style'
        style.textContent =
          '#sidebar, .sidebar { display: none !important; }' +
          '#content, .content-pane { margin-left: 0 !important; padding: 12px 24px 80px 14px !important; max-width: 100% !important; box-sizing: border-box !important; }' +
          '.doc-article-container { margin: 0 !important; max-width: 900px !important; width: 100% !important; box-sizing: border-box !important; padding: 0 !important; }' +
          '.doc-section { scroll-margin-top: 24px; max-width: 100% !important; box-sizing: border-box !important; }' +
          'html, body, .viewer { overflow-x: hidden !important; max-width: 100% !important; }'
        idoc.head.appendChild(style)
      }
    }

    // Set theme attribute
    idoc.documentElement.setAttribute('data-theme', themeRef.current)

    // Forward clicks to document to close host dropdowns
    const forwardPointer = () => {
      document.dispatchEvent(new MouseEvent('mousedown'))
      document.dispatchEvent(new Event('touchstart'))
    }
    idoc.addEventListener('mousedown', forwardPointer)
    idoc.addEventListener('touchstart', forwardPointer)

    setIframeReady(true)
  }

  // Listen for messages from docs-viewer iframe
  useEffect(() => {
    const handler = (e: MessageEvent) => {
      if (e.source !== iframeRef.current?.contentWindow) return
      const data = e.data ?? {}

      switch (data.type) {
        case 'ready':
          post({ type: 'theme', value: themeRef.current })
          if (sectionRef.current) {
            post({ type: 'navigate', id: sectionRef.current })
          }
          break

        case 'toc':
          if (Array.isArray(data.groups)) {
            setDocsGroups(data.groups as DocGroup[])
          }
          break

        case 'active':
          lastActiveRef.current = data.id ?? null
          setDocsActive(data.id ?? null, data.section ?? null)
          // Update URL search param if changed during scroll
          if (data.id && data.id !== sectionRef.current) {
            setSearchParams(
              (prev) => {
                const next = new URLSearchParams(prev)
                next.set('section', data.id)
                return next
              },
              { replace: true }
            )
          }
          break
      }
    }

    window.addEventListener('message', handler)
    return () => window.removeEventListener('message', handler)
  }, [setSearchParams])

  // Sync theme changes
  useEffect(() => {
    post({ type: 'theme', value: theme })
    const idoc = iframeRef.current?.contentDocument
    if (idoc) {
      idoc.documentElement.setAttribute('data-theme', theme)
    }
  }, [theme])

  // Scroll to section when ?section= changes for a reason other than the
  // viewer's own scroll-spy (i.e. the user clicked a nav link or a deep link).
  useEffect(() => {
    if (section && section !== lastActiveRef.current) {
      post({ type: 'navigate', id: section })
    }
  }, [section])

  return (
    <div
      className={cn(
        'flex min-h-0 flex-col h-full transition-opacity duration-300 ease-out',
        iframeReady ? 'opacity-100' : 'opacity-0'
      )}
    >
      <div className="page-header mb-2.5">
        <h1 className="page-title font-display text-2xl lg:text-3xl gradient-text">Documentation</h1>
        <p className="page-subtitle text-xs text-muted-foreground mt-0.5">Architecture, modules, API and deployment reference</p>
      </div>
      <iframe
        ref={iframeRef}
        src="/docs-viewer/index.html"
        onLoad={syncIframe}
        title="SeldonVigil Documentation"
        className="min-h-[30rem] w-full flex-1 rounded-xl border border-border bg-background lg:min-h-0"
      />
    </div>
  )
}
